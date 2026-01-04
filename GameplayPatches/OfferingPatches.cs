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
        bool inCombat,
        bool isEvent)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        // Make given card unusable if it is an item but not unlocked
        foreach (var card in __result)
        {
            var cardType = card.GetType();
            if (!Archipelago.CardToItem.TryGetValue(cardType, out var itemName)) continue;
            if (Archipelago.Instance.APSaveData.AppliedInventory.TryGetValue(
                    itemName, out var itemCount) && itemCount != 0) continue;
            card.unplayableOverride = true;
            card.unplayableOverrideIsPermanent = true;
        }
        
        if (inCombat || isEvent) return;
        
        // Add archipelago check cards
        
        var availableDecks = s.characters.Select(c => c.deckType).ToList();
        var targetCount = __result.Count;
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
                .Where(name => name.StartsWith($"{deckName} {rarityName}")).ToList();
            if (locationChoices.Count <= 0) continue;
            
            var location = locationChoices.Random(s.rngCardOfferings)!;
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
        
        // Scout proposed archipelago cards
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
        var rng = rngOverride ?? s.rngArtifactOfferings;
        var availableDecks = s.characters.Select(c => c.deckType).ToList();
        var effPools = limitPools ?? [ArtifactPool.Common];
        
        // Attempt to add a single artifact, cancel if it fails
        var deck = limitDeck ?? availableDecks.Random(rng);
        var deckName = Archipelago.ItemToDeck.First(kvp => kvp.Value == deck).Key;
        string rarityName;
        if (effPools.Contains(ArtifactPool.Boss))
            rarityName = "Boss";
        else if (effPools.Contains(ArtifactPool.Common))
            rarityName = "Common";
        else
            rarityName = "N/A";
        var locationChoices = Locations.AllMissingLocations
            .Select(address => Locations.GetLocationNameFromId(address))
            .Where(name => name.StartsWith($"{deckName} {rarityName}")).ToList();
        if (locationChoices.Count <= 0) return;
        
        var location = locationChoices.Random(s.rngCardOfferings)!;
        var artifact = rarityName == "Boss" ? new CheckLocationArtifactBoss() : new CheckLocationArtifact();
        artifact.locationName = location;
        
        // Add the artifact then remove one at random to keep count
        __result.Add(artifact);
        __result.RemoveAt(s.rngCardOfferings.NextInt() % __result.Count);

        // If it was picked, scout its location
        if (__result.Contains(artifact))
        {
            Archipelago.Instance.CheckLocationInfo(location).ContinueWith(task =>
            {
                var (itemName, slotName) = task.Result[0];
                artifact.SetTextInfo(itemName, slotName);
            });
        }
    }
}
