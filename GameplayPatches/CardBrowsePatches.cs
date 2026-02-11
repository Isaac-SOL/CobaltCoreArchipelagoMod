using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using CobaltCoreArchipelago.Actions;
using CobaltCoreArchipelago.Cards;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;

// ReSharper disable MultipleOrderBy

namespace CobaltCoreArchipelago.GameplayPatches;

[HarmonyPatch(typeof(CardBrowse), nameof(CardBrowse.GetCardList))]
public class CardBrowseListPatch
{
    internal static List<Card> pickableCardsCache = [];
    internal static List<string> apLocationsCache = [];
    internal static List<CheckLocationCard> fixedApCardsCache = [];

    internal static void PrepareCache(State s, CardBrowseAPData info)
    {
        switch (info.filterMode)
        {
            case CardBrowseAPData.FilterMode.UnlockedCardsNotInDeck:
                pickableCardsCache = GetPickableUnlockedCardsList(s);
                break;
            case CardBrowseAPData.FilterMode.FoundMissingLocations:
                Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
                
                apLocationsCache = GetPickableAPCardsList(s);
                fixedApCardsCache = apLocationsCache
                    .Select(name =>
                                name.Contains("Common") ? new CheckLocationCard { locationName = name }
                                : name.Contains("Uncommon") ? new CheckLocationCardUncommon { locationName = name }
                                : new CheckLocationCardRare { locationName = name })
                    .ToList();
                pickableCardsCache = fixedApCardsCache
                    .Cast<Card>()
                    .ToList();
                
                if (Archipelago.Instance.APSaveData.CardScoutMode == CardScoutMode.DontScout) break;
                
                Archipelago.Instance.ScoutLocationInfo(apLocationsCache.ToArray()).ContinueWith(task =>
                {
                    for (var i = 0; i < fixedApCardsCache.Count; i++)
                        fixedApCardsCache[i].LoadInfo(task.Result[i]);
                });
                CardBrowseRenderPatch.rescoutTimer = 0.0;
                break;
        }
    }
    
    // If we have some ModData on this CardBrowse, it means we override what it actually browses
    public static void Postfix(CardBrowse __instance, ref List<Card> __result)
    {
        var modDataHelper = ModEntry.Instance.Helper.ModData;
        if (!modDataHelper.TryGetModData(__instance, "AdditionalAPData", out CardBrowseAPData? _)) return;
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");

        // Just replace the cache, DO NOT clear it, it could be referencing a saved value such as cardsOwned
        __instance._listCache = pickableCardsCache;

        var orderedCards = __instance.sortMode switch
        {
            CardBrowse.SortMode.Deck => __instance._listCache
                .OrderBy(c => c.uuid)
                .OrderBy(c => c.GetLocName())
                .OrderBy(c => c.GetMeta().deck),
            
            CardBrowse.SortMode.Name => __instance._listCache
                .OrderBy(c => c.uuid)
                .OrderBy(c => c.GetLocName()),
            
            CardBrowse.SortMode.Cost => __instance._listCache
                .OrderBy(c => c.uuid)
                .OrderBy(c => c.GetLocName())
                .OrderBy(c => c.GetData(DB.fakeState).cost),
            
            CardBrowse.SortMode.Rarity => __instance._listCache
                .OrderBy(c => c.uuid)
                .OrderBy(c => c.GetLocName())
                .OrderBy(c => c.GetMeta().deck)
                .OrderBy(c => c.GetMeta().rarity),
            
            _ => null
        };

        if (orderedCards is not null) __instance._listCache = orderedCards.ToList();

        __result = __instance._listCache;
    }
    
    internal static List<Card> GetPickableUnlockedCardsList(State s) => Archipelago.Instance.APSaveData!.FoundCards
        .Select(cardType => (cardType.CreateInstance() as Card)!)
        .Where(card => s.characters.Any(c => c.deckType!.Value == card.GetMeta().deck)
                       && s.deck.All(deckCard => deckCard.GetType() != card.GetType()))
        .ToList();
    
    internal static List<Artifact> GetPickableUnlockedArtifactsList(State s) => Archipelago.Instance.APSaveData!.FoundArtifacts
        .Select(artifactType => (artifactType.CreateInstance() as Artifact)!)
        .Where(artifact => s.characters.Any(c => c.deckType!.Value == artifact.GetMeta().owner)
                           && s.artifacts.All(ownedArtifact => ownedArtifact.GetType() != artifact.GetType())
                           && !ArtifactReward.GetBlockedArtifacts(s).Contains(artifact.GetType()))
        .ToList();

    internal static List<string> GetPickableAPLocationsList(State s) => Archipelago.Instance.APSaveData!.AllSeenLocations
        .Intersect(Archipelago.Instance.Session!.Locations.AllMissingLocations
                       .Select(l => Archipelago.Instance.Session.Locations.GetLocationNameFromId(l)))
        .Where(name => s.characters
                           .Select(c => Archipelago.DeckToItem[c.deckType!.Value])
                           .Append("Basic")
                           .Any(name.StartsWith))
        .ToList();

    internal static List<string> GetPickableAPCardsList(State s) => GetPickableAPLocationsList(s)
        .Where(name => name.Contains("Card")
                       && !s.deck.Any(card => card is CheckLocationCard apCard && apCard.locationName == name))
        .ToList();

    internal static List<string> GetPickableAPArtifactsList(State s) => GetPickableAPLocationsList(s)
        .Where(name => name.Contains("Artifact"))
        .ToList();
}

[HarmonyPatch(typeof(CardBrowse), nameof(CardBrowse.Render))]
public static class CardBrowseRenderPatch
{
    // Change the title that appears at the top of the card browser
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> storedInstructions = new(instructions);

        var seqMatched = new SequenceBlockMatcher<CodeInstruction>(storedInstructions)
            .Find(
                ILMatches.Stloc(3)
            )
            .PointerMatcher(SequenceMatcherRelativeElement.First)
            .Insert(
                SequenceMatcherPastBoundsDirection.Before,
                SequenceMatcherInsertionResultingBounds.ExcludingInsertion,
                // default string is currently on the stack (ldloc right before the stloc)
                CodeInstruction.LoadArgument(0),  // Load instance
                // Inject our title function (takes cares of checking whether the title should be changed at all)
                CodeInstruction.Call((string defaultTitle, CardBrowse cardBrowse) => GetCardBrowseTitle(defaultTitle, cardBrowse))
            );
        
        return seqMatched.AllElements();
    }

    public static string GetCardBrowseTitle(string defaultTitle, CardBrowse cardBrowse)
    {
        var modDataHelper = ModEntry.Instance.Helper.ModData;
        if (!modDataHelper.TryGetModData(cardBrowse, "AdditionalAPData", out CardBrowseAPData? data))
            return defaultTitle;

        return data!.filterMode switch
        {
            CardBrowseAPData.FilterMode.UnlockedCardsNotInDeck =>
                string.Format(ModEntry.Instance.Localizations.Localize(["cardBrowse", "bootOptionUnlockedCardTitle"]),
                              CardBrowseListPatch.pickableCardsCache.Count),
            CardBrowseAPData.FilterMode.FoundMissingLocations =>
                string.Format(ModEntry.Instance.Localizations.Localize(["cardBrowse", "eventMissedAPCardTitle"]),
                              CardBrowseListPatch.pickableCardsCache.Count),
            _ => defaultTitle
        };
    }
    
    // Auto rescout AP cards
    
    internal static double rescoutTimer = 0.0;

    public static void Postfix(CardBrowse __instance, G g)
    {
        var modDataHelper = ModEntry.Instance.Helper.ModData;
        if (!modDataHelper.TryGetModData(__instance, "AdditionalAPData", out CardBrowseAPData? data))
            return;
        
        if (data!.filterMode != CardBrowseAPData.FilterMode.FoundMissingLocations) return;
        
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        
        if (Archipelago.Instance.APSaveData.CardScoutMode == CardScoutMode.DontScout) return;

        // Recheck not scouted AP checks every 5 seconds
        if (rescoutTimer > 5.0)
        {
            rescoutTimer -= 5.0;

            var checkCards = CardBrowseListPatch.fixedApCardsCache;
            var locations = checkCards.Select(card => card.locationName).ToArray();

            if (locations.Length > 0)
            {
                Archipelago.Instance.ScoutLocationInfo(locations).ContinueWith(task =>
                {
                    for (var i = 0; i < checkCards.Count; i++)
                        checkCards[i].LoadInfo(task.Result[i]);
                });
            }
        }
        rescoutTimer += g.dt;
    }
}
