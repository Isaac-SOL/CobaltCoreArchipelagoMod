using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HarmonyLib;
using Microsoft.Extensions.Logging;

namespace CobaltCoreArchipelago.GameplayPatches;


[HarmonyPatch(typeof(State), nameof(State.EndRun))]
public static class EndRunShufflePatch
{
    public static readonly HashSet<Type> OffensiveCards =
    [
        // Dizzy
        typeof(BlockShot), typeof(Deflection), typeof(AcidCannon), typeof(CorrosionBeam),
        // Riggs
        typeof(DrawCannon), typeof(EvasiveShot), typeof(Whiplash), typeof(ChargeBeam), typeof(HandCannon),
        // Peri
        typeof(Lunge), typeof(BigGun), typeof(MultiShot), typeof(WaveCharge), typeof(Barrage), typeof(FumeCannon),
        typeof(EndlessMagazine),
        // Isaac
        typeof(AttackDroneCard), typeof(MissileLaunchCard), typeof(ShiftShot), typeof(SpaceMineCard),
        // Drake
        typeof(EMPSlug), typeof(HESlug), typeof(AggressiveArmoring), typeof(FlashPoint), typeof(Heatwave),
        typeof(SearCard), typeof(VolatileVaporCard), typeof(FreezeDry),
        // Max
        typeof(DiceRoll), typeof(RerouteCard), typeof(ShuffleShot), typeof(LazyBarrage), typeof(MaxTrashGeneration),
        // Books
        typeof(Glimmershot), typeof(MageHand), typeof(MiningDrillCard), typeof(BloodstoneBolt)
    ];
    
    public static readonly HashSet<Type> GeneratorCards =
    [
        // Drake
        typeof(EMPSlug), typeof(HESlug), typeof(Firewall), typeof(SearCard), typeof(ThermalBattery),
        // Books
        typeof(MiningDrillCard), typeof(Shardsource), typeof(UnpoweredShardCard), typeof(MineralDeposit),
        typeof(ShardPack), typeof(MedusaField), typeof(PerfectSpecimen), typeof(QuantumQuarryCard)
    ];

    static void Postfix()
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        Archipelago.Instance.APSaveData.ThisRunSeenLocations.Clear();
        APSaveData.Save();
        
        if (Archipelago.InstanceSlotData.RandomizeStartingCards == FrequencyShuffleMode.EveryRun)
            ShuffleStarterSets();
        if (Archipelago.InstanceSlotData.ShuffleShipParts == FrequencyShuffleMode.EveryRun)
            ShuffleStartingShips();
    }

    internal static void ShuffleStartingShips()
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        var rand = Archipelago.Instance.APSaveData.ShipShuffleRand;
        // Make sure the seed does not change if not in EveryRun mode
        if (Archipelago.InstanceSlotData.ShuffleShipParts == FrequencyShuffleMode.EveryRun)
            Archipelago.Instance.APSaveData.PrevShipShuffleSeed = rand.seed;
        else
            rand.seed = Archipelago.Instance.APSaveData.PrevShipShuffleSeed;
        ModEntry.Instance.Logger.LogInformation("Shuffling ships with seed: {shipSeed}", rand.seed);
        foreach (var shipName in StarterShip.ships.Keys)
        {
            var shuffledParts = ModEntry.BaseShips[shipName].ship.parts.Shuffle(rand);
            StarterShip.ships[shipName].ship.parts = Mutil.DeepCopy(new List<Part>(shuffledParts));
        }
        APSaveData.Save();
    }

    internal static void ShuffleStarterSets()
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");

        // If not every run, we just pick from the random starting cards
        if (Archipelago.InstanceSlotData.ShuffleShipParts != FrequencyShuffleMode.EveryRun
            || !Archipelago.Instance.APSaveData.FoundCards.Any()) // Shuffle every run, fallback on first connection
        {
            foreach (var deck in Archipelago.ItemToDeck.Values)
            {
                if (deck == Deck.colorless) continue;
                var set = StarterDeck.starterSets[deck];
                var soloSet = SoloStarterDeck.soloStarterSets[deck];
                set.cards = [];
                soloSet.cards.RemoveAll(c => !IsBasic(c));
                foreach (var card in Archipelago.InstanceSlotData.DeckStartingCards[deck])
                {
                    set.cards.Add((Card)card.CreateInstance());
                    soloSet.cards.InsertRange(0, [(Card)card.CreateInstance(), (Card)card.CreateInstance()]);
                }
            }
            return;
        }
        
        // If every run, we use the randomizer
        var rand = Archipelago.Instance.APSaveData.ShipShuffleRand;
        Archipelago.Instance.APSaveData.PrevStartingCardsSeed = rand.seed;
        ModEntry.Instance.Logger.LogInformation("Shuffling starting cards with seed: {cardSeed}", rand.seed);
        var unlockedCards = Archipelago.Instance.APSaveData.FoundCards
            .Select(t => t.CreateInstance())
            .Cast<Card>()
            .ToList();
        foreach (var deck in Archipelago.ItemToDeck.Values)
        {
            if (deck == Deck.colorless) continue;
            var set = StarterDeck.starterSets[deck];
            var soloSet = SoloStarterDeck.soloStarterSets[deck];
            var possibleCards = unlockedCards
                .Where(c => c.GetMeta().deck == deck)
                .ToList();
            var offensiveCards = possibleCards
                .Where(c => OffensiveCards.Contains(c.GetType()))
                .ToList();
            var generatorCards = possibleCards
                .Where(c => GeneratorCards.Contains(c.GetType()))
                .ToList();
            var offC = offensiveCards.Random(rand)!;
            var effSecondCards = generatorCards.Count > 0 && !generatorCards.Contains(offC)
                ? generatorCards
                : possibleCards;
            effSecondCards.Remove(offC);
            var secC = effSecondCards.Random(rand)!;
            set.cards = [offC, secC];
            var savedBasics = soloSet.cards.Where(IsBasic).ToList();
            soloSet.cards.Clear();
            soloSet.cards.AddRange([offC, secC]);
            soloSet.cards.AddRange(possibleCards
                                          .Where(c => c != offC && c != secC)
                                          .Shuffle()
                                          .Concat([offC, secC]) // Fallback if no more cards
                                          .Take(2));
            soloSet.cards.AddRange(savedBasics);
        }
        APSaveData.Save();
    }

    private static bool IsBasic(Card c) =>
        c is CannonColorless or DodgeColorless or BasicShieldColorless or DroneshiftColorless;
}