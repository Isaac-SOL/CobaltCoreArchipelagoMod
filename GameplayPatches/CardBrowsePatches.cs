using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using CobaltCoreArchipelago.Actions;
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

    internal static void PrepareCache(State s, CardBrowseAPData info)
    {
        switch (info.filterMode)
        {
            case CardBrowseAPData.FilterMode.UnlockedCardsNotInDeck:
                pickableCardsCache = GetPickableUnlockedCardsList(s);
                break;
            case CardBrowseAPData.FilterMode.FoundMissingLocations:
                break;
        }
    }
    
    // If we have some ModData on this CardBrowse, it means we override what it actually browses
    public static void Postfix(CardBrowse __instance, ref List<Card> __result)
    {
        var modDataHelper = ModEntry.Instance.Helper.ModData;
        if (!modDataHelper.TryGetModData(__instance, "AdditionalAPData", out CardBrowseAPData? data)) return;
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");

        switch (data!.filterMode)
        {
            case CardBrowseAPData.FilterMode.UnlockedCardsNotInDeck:
                // Just replace the cache, DO NOT clear it, it could be referencing a saved value such as cardsOwned
                __instance._listCache = pickableCardsCache;
                break;
            case CardBrowseAPData.FilterMode.FoundMissingLocations:
                break;
        }

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
}
