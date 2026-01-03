using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Archipelago.MultiClient.Net.Helpers;
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
        if (inCombat || isEvent) return;
        
        var availableDecks = s.characters.Select(c => c.deckType).ToList();
        var targetCount = __result.Count;
        List<Card> archipelagoCards = [];
        var attempts = 0;
        while (archipelagoCards.Count < targetCount && attempts++ < 100)
        {
            var deck = limitDeck ?? availableDecks.Random(s.rngCardOfferings);
            var deckName = Archipelago.ItemToDeck.First(kvp => kvp.Value == deck).Key;
            var rarity = rarityOverride ?? CardReward.GetRandomRarity(s.rngCardOfferings, battleType);
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
            CheckLocationCard card = new();
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
}