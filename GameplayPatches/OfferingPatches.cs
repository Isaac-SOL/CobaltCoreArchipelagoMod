using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Archipelago.MultiClient.Net.Helpers;
using CobaltCoreArchipelago.Artifacts;
using CobaltCoreArchipelago.Cards;
using CobaltCoreArchipelago.Features;
using HarmonyLib;
using Microsoft.Extensions.Logging;

namespace CobaltCoreArchipelago.GameplayPatches;

public class OfferingPatches;

[HarmonyPatch(typeof(CardReward), nameof(CardReward.GetOffering))]
public class CardOfferingPatch
{
    private static ILocationCheckHelper Locations
    {
        get
        {
            Debug.Assert(Archipelago.Instance.Session != null, "Archipelago.Instance.Session != null");
            return Archipelago.Instance.Session.Locations;
        }
    }

    static void Postfix(
        ref List<Card> __result,
        State s,
        Deck? limitDeck,
        BattleType battleType,
        Rarity? rarityOverride,
        bool? overrideUpgradeChances,
        bool makeAllCardsTemporary,
        bool inCombat,
        int discount,
        bool isEvent)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        
        if (!Archipelago.InstanceSlotData.ShuffleCards) return;
        
        // PHASE 1: Make each given card unusable if it is an item but not unlocked
        
        // REMOVED: This is now handled with OnGetDynamicInnateCardTraitOverrides in ModEntry
        
        // PHASE 2: Add one found card if the option permits it
        
        var availableDecks = s.characters.Select(c => c.deckType).ToList();
        var targetCount = __result.Count;
        if (Archipelago.InstanceSlotData.GetMoreFoundItems)
        {
            var bonusCardAttempts = 0;
            while (__result.Count < targetCount + 1 && bonusCardAttempts++ < 5)
            {
                var deck = limitDeck ?? availableDecks.Random(s.rngCardOfferings);
                var rarity = rarityOverride ?? CardReward.GetRandomRarity(s.rngCardOfferings, battleType);
                var pickableCards = DB.releasedCards.Where(c =>
                {
                    if (!Archipelago.Instance.APSaveData.HasCard(c.GetType())) return false;
                    var meta = c.GetMeta();
                    if (meta.rarity != rarity) return false;
                    return deck is not null
                           && deck == meta.deck
                           && meta is { dontOffer: false, unreleased: false };
                }).ToList();
                if (pickableCards.Count == 0) continue;
                var cardType = pickableCards.Random(s.rngCardOfferings).GetType();
                if (__result.Any(c => c.GetType() == cardType)) continue;
                var card = (Card)cardType.CreateInstance();
                card.drawAnim = 1.0;
                card.upgrade = CardReward.GetUpgrade(s, s.rngCardOfferings, s.map, card, s.GetDifficulty() >= 1 ? 0.5 : 1.0, overrideUpgradeChances);
                card.flipAnim = 1.0;
                if (makeAllCardsTemporary)
                    card.temporaryOverride = true;
                if (discount != 0)
                    card.discount = discount;
                __result.Add(card);
                __result.RemoveAt(s.rngCardOfferings.NextInt() % __result.Count);
                __result = __result.Shuffle(s.rngCardOfferings).ToList();
            }
        }
        
        if (inCombat || isEvent) return;
        
        // PHASE 3: Add archipelago check cards
        
        var archipelagoCards = new List<Card>();
        var pickedLocations = new List<string>();
        var attempts = 0;
        while (archipelagoCards.Count < targetCount && attempts++ < 5)
        {
            var deck = limitDeck ?? availableDecks.Random(s.rngCardOfferings);
            var deckName = Archipelago.ItemToDeck.First(kvp => kvp.Value == deck).Key;
            var rarity = rarityOverride ?? GetRandomAPCheckRarity(s, battleType);
            var rarityName = rarity switch
            {
                Rarity.common => "Common",
                Rarity.uncommon => "Uncommon",
                _ => "Rare"
            };

            Card card;
            if (Archipelago.Instance.APSaveData.DeathLinkActive
                && rarity == Rarity.rare && s.rngCardOfferings.NextInt() % 100 > 95)
            {
                // Sometimes replace rare cards with a DeathLinkBoros
                card = new DeathLinkBoros();
            }
            else
            {
                // But most of the time we add an actual check card with a set location
                var locationChoices = Locations.AllMissingLocations
                    .Select(address => Locations.GetLocationNameFromId(address))
                    .Where(name => name.StartsWith($"{deckName} {rarityName} Card") && !pickedLocations.Contains(name))
                    .ToList();
                if (locationChoices.Count <= 0) continue;

                var location = NotSoRandomManager.RandomLocation(locationChoices, s.rngCardOfferings);
                card = rarity switch
                {
                    Rarity.common => new CheckLocationCard(),
                    Rarity.uncommon => new CheckLocationCardUncommon(),
                    _ => new CheckLocationCardRare()
                };
                (card as CheckLocationCard)!.locationName = location;
                pickedLocations.Add(location);
            }
            card.drawAnim = 1.0;
            card.upgrade = CardReward.GetUpgrade(s, s.rngCardOfferings, s.map, card, s.GetDifficulty() >= 1 ? 0.5 : 1.0, overrideUpgradeChances);
            card.flipAnim = 1.0;
            archipelagoCards.Add(card);
        }
        
        // Concatenate both picks and cull at random to keep normal offering count
        __result.AddRange(archipelagoCards);
        while (__result.Count > targetCount)
            __result.RemoveAt(s.rngCardOfferings.NextInt() % __result.Count);
        __result = __result.Shuffle(s.rngCardOfferings).ToList();
        
        // Scout proposed archipelago cards if the options allow for it
        if (Archipelago.Instance.APSaveData.CardScoutMode == CardScoutMode.DontScout) return;
        
        var checkCards = __result
            .Where(card => card is CheckLocationCard)
            .Cast<CheckLocationCard>()
            .ToList();
        var locations = checkCards.Select(card => card.locationName).ToArray();
        NotSoRandomManager.AddSeenLocations(locations);
        APSaveData.Save();
        
        Archipelago.Instance.ScoutLocationInfo(locations).ContinueWith(task =>
        {
            for (var i = 0; i < checkCards.Count; i++)
            {
                var info = task.Result[i];
                if (info is null)
                    checkCards[i].SetTextInfo("[]", "[]", APColors.Trap);
                else
                    checkCards[i].SetTextInfo(info.ItemName, info.Player.Name, info.GetColor());
            }
        });
    }

    private static Rarity GetRandomAPCheckRarity(State s, BattleType battleType)
    {
        var roll = s.rngCardOfferings.Next();
        var power = s.map switch
        {
            MapFirst => 2.0,
            MapLawless => 1.0,
            _ => 0.5
        };
        if (Archipelago.InstanceSlotData.RarerChecksLater)
            roll = Math.Pow(roll, power);
        return battleType switch
        {
            BattleType.Elite => Mutil.Roll(roll, (0.35, Rarity.common), (0.45, Rarity.uncommon), (0.2, Rarity.rare)),
            BattleType.Boss => Rarity.rare,
            _ => Mutil.Roll(roll, (0.75, Rarity.common), (0.2, Rarity.uncommon), (0.05, Rarity.rare))
        };
    }
}

[HarmonyPatch(typeof(ArtifactReward), nameof(ArtifactReward.GetOffering))]
public class ArtifactOfferingPatch
{
    private static ILocationCheckHelper Locations
    {
        get
        {
            Debug.Assert(Archipelago.Instance.Session != null, "Archipelago.Instance.Session != null");
            return Archipelago.Instance.Session.Locations;
        }
    }

    static void Postfix(
        ref List<Artifact> __result,
        State s,
        int count,
        Deck? limitDeck = null,
        List<ArtifactPool>? limitPools = null,
        Rand? rngOverride = null)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        
        if (!Archipelago.InstanceSlotData.ShuffleArtifacts) return;

        // PHASE 1: Remove each artifact if it is an item but not unlocked
        
        var artifactsTemp = new List<Artifact>(__result);
        var selectedTypes = new List<Type>();  // Save in a list to exclude in next phase
        __result.Clear();
        foreach (var artifact in artifactsTemp)
        {
            var artifactType = artifact.GetType();
            selectedTypes.Add(artifactType);
            // Ensure that the artifact is not an item OR we have it in our inventory
            if (!Archipelago.ArtifactToItem.TryGetValue(artifactType, out var itemName)
                || Archipelago.Instance.APSaveData.HasItem(itemName))
                __result.Add(artifact);
        }
        
        // PHASE 2: Add one found artifact if the option permits it
        
        if (Archipelago.InstanceSlotData.GetMoreFoundItems)
        {
            var charDecks = new List<Deck?>();
            foreach (var character in s.characters)
            {
                charDecks.Add(character.deckType);
                if (character.deckType is not null && character.deckType == Deck.colorless)
                    charDecks.Add(Deck.catartifact);
            }
            charDecks.Add(Deck.colorless);
            limitPools ??= [ArtifactPool.Common];
            var ownedTypes = s.EnumerateAllArtifacts().Select(art => art.GetType()).ToHashSet();
            var blocklist = ArtifactReward.GetBlockedArtifacts(s);
            
            var bonusArtifactAttempts = 0;
            while (bonusArtifactAttempts++ < 5)
            {
                var pickedArtifact = DB.artifacts.Values.Shuffle(rngOverride ?? s.rngArtifactOfferings).FirstOrDefault(ty =>
                {
                    if (!Archipelago.Instance.APSaveData.HasArtifact(ty)) return false;
                    if (blocklist.Contains(ty)) return false;
                    if (!DB.artifactMetas.TryGetValue(ty.Name, out var meta)) meta = new ArtifactMeta();
                    if (limitDeck is not null && limitDeck != meta.owner) return false;
                    if (!limitPools.Any(p => meta.pools.Contains(p))) return false;
                    return !selectedTypes.Contains(ty)
                           && !ownedTypes.Contains(ty)
                           && charDecks.Contains(meta.owner);
                });
                // If we found a valid artifact, add it and immediately go to the next phase
                if (pickedArtifact is not null)
                {
                    __result.Add((Artifact)pickedArtifact.CreateInstance());
                    break;
                }
            }
        }
        
        // PHASE 3: Add archipelago check artifacts

        var rng = rngOverride ?? s.rngArtifactOfferings;
        var availableDecks = s.characters.Select(c => c.deckType).ToList();
        availableDecks.Add(Deck.tooth);  // Just a dummy deck, not present in ItemToDeck
        var effPools = limitPools ?? [ArtifactPool.Common];
        var rarityName = effPools.Contains(ArtifactPool.Boss) ?   "Boss "
                       : effPools.Contains(ArtifactPool.Common) ? ""
                       :                                          "N/A ";
        var archipelagoArtifacts = new List<CheckLocationArtifact>();
        var pickedLocations = new List<string>();
        var apArtifactsAttempts = 0;
        while (apArtifactsAttempts++ < 10 && archipelagoArtifacts.Count < count)
        {
            var deck = limitDeck ?? availableDecks.Random(rng);
            var deckName = Archipelago.ItemToDeck.FirstOrDefault(kvp => kvp.Value == deck, new KeyValuePair<string, Deck>("Basic", Deck.tooth)).Key;
            var locationChoices = Locations.AllMissingLocations
                .Select(address => Locations.GetLocationNameFromId(address))
                .Where(name => name.StartsWith($"{deckName} {rarityName}Artifact") && !pickedLocations.Contains(name))
                .ToList(); // Note the absence of space before "Artifact"
            if (locationChoices.Count <= 0) continue;

            var location = NotSoRandomManager.RandomLocation(locationChoices, rng);
            var newArtifact = rarityName == "Boss " ? new CheckLocationArtifactBoss() : new CheckLocationArtifact();
            newArtifact.locationName = location;
            
            archipelagoArtifacts.Add(newArtifact);
            pickedLocations.Add(location);
        }
        
        // Concatenate both picks and cull at random to keep normal offering count
        __result.AddRange(archipelagoArtifacts);
        while (__result.Count > count)
            __result.RemoveAt(s.rngCardOfferings.NextInt() % __result.Count);
        __result = __result.Shuffle(s.rngCardOfferings).ToList();
        
        // Add seen locations to the NotSoRandomManager so that they won't be seen for a while
        var checkArtifacts = __result
            .Where(artifact => artifact is CheckLocationArtifact)
            .Cast<CheckLocationArtifact>()
            .ToList();
        var locations = checkArtifacts.Select(card => card.locationName).ToArray();
        NotSoRandomManager.AddSeenLocations(locations);
        APSaveData.Save();
        
        // Scout proposed archipelago artifacts if the options allow for it
        if (Archipelago.Instance.APSaveData.CardScoutMode == CardScoutMode.DontScout) return;
        
        Archipelago.Instance.ScoutLocationInfo(locations).ContinueWith(task =>
        {
            for (var i = 0; i < checkArtifacts.Count; i++)
            {
                var info = task.Result[i];
                if (info is null)
                    checkArtifacts[i].SetTextInfo("[]", "[]", APColors.Trap);
                else
                    checkArtifacts[i].SetTextInfo(info.ItemName, info.Player.Name, info.GetColor());
            }
        });
    }
}

// TODO this causes a crash "duplicate ui key" when the player has multiple locked / archipelago artifacts
// [HarmonyPatch(typeof(State), nameof(State.AddNonCharacterArtifact))]
// class AddNonCharacterArtifactPatch
// {
//     // Allow having multiple of the same artifact (for archipelago and locked artifacts)
//     public static bool Prefix(State __instance, Artifact artifact)
//     {
//         __instance.artifacts.Add(artifact);
//         return false;
//     }
// }
