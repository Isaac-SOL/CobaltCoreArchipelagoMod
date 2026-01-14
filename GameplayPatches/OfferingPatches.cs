using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Archipelago.MultiClient.Net.Helpers;
using CobaltCoreArchipelago.Artifacts;
using CobaltCoreArchipelago.Cards;
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
        
        foreach (var card in __result)
        {
            var cardType = card.GetType();
            if (!Archipelago.CardToItem.TryGetValue(cardType, out var itemName)) continue;
            if (Archipelago.Instance.APSaveData.AppliedInventory.TryGetValue(
                    itemName, out var itemCount) && itemCount != 0) continue;
            card.unplayableOverride = true;
            card.unplayableOverrideIsPermanent = true;
        }
        
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
        
        List<Card> archipelagoCards = [];
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
            var locationChoices = Locations.AllMissingLocations
                .Select(address => Locations.GetLocationNameFromId(address))
                .Where(name => name.StartsWith($"{deckName} {rarityName} Card")).ToList();
            if (locationChoices.Count <= 0) continue;
            
            var location = locationChoices.Random(s.rngCardOfferings)!;
            // Make sure we don't give multiple of the same location in one offering. The AP package doesn't like that
            if (archipelagoCards.Any(prevCard => prevCard is CheckLocationCard prevAPCard
                                                 && prevAPCard.locationName == location))
                continue;
            
            var card = rarity switch
            {
                Rarity.common => new CheckLocationCard(),
                Rarity.uncommon => new CheckLocationCardUncommon(),
                _ => new CheckLocationCardRare()
            };
            card.locationName = location;
            card.drawAnim = 1.0;
            card.upgrade = CardReward.GetUpgrade(s, s.rngCardOfferings, s.map, card, s.GetDifficulty() >= 1 ? 0.5 : 1.0, overrideUpgradeChances);
            card.flipAnim = 1.0;
            archipelagoCards.Add(card);
        }
        
        // Concatenate both picks and cull at random to keep normal offering count
        __result.AddRange(archipelagoCards);
        while (__result.Count > targetCount)
        {
            __result.RemoveAt(s.rngCardOfferings.NextInt() % __result.Count);
        }
        __result = __result.Shuffle(s.rngCardOfferings).ToList();
        
        // Scout proposed archipelago cards if the options allow for it
        if (Archipelago.Instance.APSaveData.CardScoutMode == CardScoutMode.DontScout) return;
        
        var checkCards = __result.Where(card => card is CheckLocationCard).Cast<CheckLocationCard>().ToList();
        var locations = checkCards.Select(card => card.locationName).ToArray();
        
        Archipelago.Instance.CheckLocationInfo(locations).ContinueWith(task =>
        {
            for (var i = 0; i < checkCards.Count; i++)
            {
                var (itemName, slotName) = task.Result[i];
                checkCards[i].SetTextInfo(itemName, slotName);
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

        var startArtifacts = __result.Aggregate("Start artifacts: ", (current, artifact) => current + $"{artifact.Name()}, ");
        ModEntry.Instance.Logger.LogInformation("{artifacts}", startArtifacts);

        // PHASE 1: Make given artifact unusable if it is an item but not unlocked
        
        var artifactsTemp = new List<Artifact>(__result);
        var selectedTypes = new List<Type>();  // Save in a list to exclude in next phase
        __result.Clear();
        foreach (var artifact in artifactsTemp)
        {
            var artifactType = artifact.GetType();
            selectedTypes.Add(artifactType);
            if (Archipelago.ArtifactToItem.TryGetValue(artifactType, out var itemName))
            {
                if (!Archipelago.Instance.APSaveData.AppliedInventory.TryGetValue(
                        itemName, out var itemCount) || itemCount == 0)
                {
                    var lockedArtifact = artifact.GetMeta().pools.Contains(ArtifactPool.Boss)
                        ? new LockedArtifactBoss()
                        : new LockedArtifact();
                    lockedArtifact.SetUnderlyingArtifact(artifact);
                    __result.Add(lockedArtifact);
                    continue;
                }
            }
            __result.Add(artifact);
        }

        var p1Artifacts = __result.Aggregate("Phase 1 artifacts: ", (current, artifact) => current + $"{artifact.Name()}, ");
        ModEntry.Instance.Logger.LogInformation("{artifacts}", p1Artifacts);
        
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
                // If we found a valid artifact, add it, remove at random and go to the next phase
                if (pickedArtifact is null) continue;
                __result.Add((Artifact)pickedArtifact.CreateInstance());
                __result.RemoveAt(s.rngArtifactOfferings.NextInt() % __result.Count);
                __result = __result.Shuffle(s.rngArtifactOfferings).ToList();
                break;
            }
        }

        var p2Artifacts = __result.Aggregate("Phase 2 artifacts: ", (current, artifact) => current + $"{artifact.Name()}, ");
        ModEntry.Instance.Logger.LogInformation("{artifacts}", p2Artifacts);
        
        // PHASE 3: Attempt to add a single archipelago check artifact, cancel if it fails
        
        var rng = rngOverride ?? s.rngArtifactOfferings;
        var availableDecks = s.characters.Select(c => c.deckType).ToList();
        availableDecks.Add(Deck.tooth);  // Just a dummy deck, not present in ItemToDeck
        var effPools = limitPools ?? [ArtifactPool.Common];
        var deck = limitDeck ?? availableDecks.Random(rng);
        var deckName = Archipelago.ItemToDeck.FirstOrDefault(kvp => kvp.Value == deck, new KeyValuePair<string, Deck>("Basic", Deck.tooth)).Key;
        string rarityName;
        if (effPools.Contains(ArtifactPool.Boss))
            rarityName = "Boss ";
        else if (effPools.Contains(ArtifactPool.Common))
            rarityName = "";
        else
            rarityName = "N/A ";
        var locationChoices = Locations.AllMissingLocations
            .Select(address => Locations.GetLocationNameFromId(address))
            .Where(name => name.StartsWith($"{deckName} {rarityName}Artifact")).ToList(); // Note the absence of space before "Artifact"
        if (locationChoices.Count <= 0) return;
        
        var location = locationChoices.Random(s.rngArtifactOfferings)!;
        var newArtifact = rarityName == "Boss " ? new CheckLocationArtifactBoss() : new CheckLocationArtifact();
        newArtifact.locationName = location;
        
        // Add the artifact then remove one at random to keep count
        __result.Add(newArtifact);
        __result.RemoveAt(s.rngArtifactOfferings.NextInt() % __result.Count);
        __result = __result.Shuffle(s.rngArtifactOfferings).ToList();

        var p3Artifacts = __result.Aggregate("Phase 3 artifacts: ", (current, artifact) => current + $"{artifact.Name()}, ");
        ModEntry.Instance.Logger.LogInformation("{artifacts}", p3Artifacts);

        // If it was picked, scout its location if the options allow for it
        if (Archipelago.Instance.APSaveData.CardScoutMode == CardScoutMode.DontScout) return;
        
        if (__result.Contains(newArtifact))
        {
            Archipelago.Instance.CheckLocationInfo(location).ContinueWith(task =>
            {
                var (itemName, slotName) = task.Result[0];
                newArtifact.SetTextInfo(itemName, slotName);
            });
        }
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
