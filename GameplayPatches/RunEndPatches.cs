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

    static void Postfix(State __instance)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        Archipelago.Instance.APSaveData.ThisRunSeenLocations.Clear();
        APSaveData.Save();
        
        if (Archipelago.InstanceSlotData.RandomizeStartingCards == FrequencyShuffleMode.EveryRun)
        {
            ShuffleStarterSetsInSave(__instance.rngActions);
            ApplyShuffledStarterSets();
        }
        else if (Archipelago.InstanceSlotData.RandomizeStartingCards == FrequencyShuffleMode.Off)
        {
            ApplyNonRandomizedSoloSets();
        }
        
        if (Archipelago.InstanceSlotData.ShuffleShipParts == FrequencyShuffleMode.EveryRun)
        {
            ShuffleStartingShipsInSave(__instance.rngActions);
            ApplyShuffledStartingShips();
        }
        
        if (Archipelago.InstanceSlotData.ModifiersMode is not (ModifierShuffleMode.Immediate
            or ModifierShuffleMode.Off))
        {
            PickNextModifiersInSave(__instance, __instance.rngActions);
        }
    }

    internal static void ShuffleStartingShipsInSave(Rand rand)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        ModEntry.Instance.Logger.LogInformation("Shuffling starting ships with seed: {shipSeed}", rand.seed);
        foreach (var shipName in StarterShip.ships.Keys)
        {
            var baseParts = ModEntry.BaseShips[shipName].ship.parts;
            var shuffledOrder = Enumerable.Range(0, baseParts.Count).Shuffle(rand).ToList();
            Archipelago.Instance.APSaveData.NextShipRando[shipName] = shuffledOrder;
        }
        APSaveData.Save();
    }

    internal static void ApplyShuffledStartingShips()
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        ModEntry.Instance.Logger.LogInformation("Applying shuffled ships");
        foreach (var shipName in StarterShip.ships.Keys)
        {
            var baseParts = ModEntry.BaseShips[shipName].ship.parts;
            var shuffledOrder = Archipelago.Instance.APSaveData.NextShipRando[shipName];
            var shuffledParts = shuffledOrder.Select(i => baseParts[i]).ToList();
            StarterShip.ships[shipName].ship.parts = Mutil.DeepCopy(shuffledParts);
        }
    }

    internal static void ShuffleStarterSetsInSaveFromSlotData()
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        ModEntry.Instance.Logger.LogInformation("Shuffling starting cards from slot data");
        foreach (var deck in Archipelago.ItemToDeck.Values)
        {
            if (deck == Deck.colorless) continue;
            var cardTypes = Archipelago.InstanceSlotData.DeckStartingCards[deck];
            Archipelago.Instance.APSaveData.NextCardRando[deck] = cardTypes
                .Concat(cardTypes)
                .Select(type => type.Name)
                .ToList();
        }
        APSaveData.Save();
    }

    internal static void ShuffleStarterSetsInSave(Rand rand)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        ModEntry.Instance.Logger.LogInformation("Shuffling starting cards with seed: {cardSeed}", rand.seed);
        var unlockedCards = Archipelago.Instance.APSaveData.FoundCards
            .Select(t => t.CreateInstance())
            .Cast<Card>()
            .ToList();
        foreach (var deck in Archipelago.ItemToDeck.Values)
        {
            if (deck == Deck.colorless) continue;
            var defaultStartingCards = Archipelago.InstanceSlotData.DeckStartingCards[deck];
            var possibleCards = unlockedCards.Where(c => c.GetMeta().deck == deck).ToList();
            var offensiveCards = possibleCards.Where(c => OffensiveCards.Contains(c.GetType())).ToList();
            var generatorCards = possibleCards.Where(c => GeneratorCards.Contains(c.GetType())).ToList();
            var offC = RandomOrNull(offensiveCards, rand) ?? (Card)defaultStartingCards[0].CreateInstance();
            var effSecondCards = generatorCards.Count > 0 && !generatorCards.Contains(offC)
                ? generatorCards
                : possibleCards;
            effSecondCards.Remove(offC);
            var secC = RandomOrNull(effSecondCards, rand) ?? (Card)defaultStartingCards[1].CreateInstance();
            var startKeys = new List<string> { offC.Key(), secC.Key() };
            Archipelago.Instance.APSaveData.NextCardRando[deck] = startKeys
                .Concat(possibleCards
                            .Select(c => c.Key())
                            .Except(startKeys)
                            .Shuffle()
                            .Concat(startKeys)
                            .Take(2))
                .ToList();
        }
        APSaveData.Save();
    }

    internal static void ApplyShuffledStarterSets()
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        ModEntry.Instance.Logger.LogInformation("Applying shuffled starting cards");
        foreach (var deck in Archipelago.ItemToDeck.Values)
        {
            if (deck == Deck.colorless) continue;
            var startingCards = Archipelago.Instance.APSaveData.NextCardRando[deck];
            var set = StarterDeck.starterSets[deck];
            var soloSet = SoloStarterDeck.soloStarterSets[deck];
            set.cards = startingCards
                .Take(2)
                .Select(key => (Card)DB.cards[key].CreateInstance())
                .ToList();
            var soloBasics = soloSet.cards.Where(IsBasic).ToList();
            soloSet.cards = startingCards
                .Take(4)
                .Select(key => (Card)DB.cards[key].CreateInstance())
                .Concat(soloBasics)
                .ToList();
        }
    }

    internal static void ApplyNonRandomizedSoloSets()
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        ModEntry.Instance.Logger.LogInformation("Applying tweak for non-random starting cards with less than 3 characters");
        foreach (var deck in Archipelago.ItemToDeck.Values)
        {
            if (deck == Deck.colorless) continue;
            var soloSet = SoloStarterDeck.soloStarterSets[deck];
            var soloBasics = soloSet.cards.Where(IsBasic).ToList();
            var soloSpecs = soloSet.cards
                .Where(card => !IsBasic(card) && Archipelago.Instance.APSaveData.HasCardOrNotAP(card.GetType()))
                .ToList();
            soloSpecs.AddRange(new List<Card>(soloSpecs));
            soloSpecs = soloSpecs.Take(4).ToList();
            
            soloSet.cards = soloSpecs
                .Select(card => card.CopyWithNewId())
                .Concat(soloBasics)
                .ToList();
        }
    }

    internal static void PickNextModifiersInSave(State state, Rand rand)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        ModEntry.Instance.Logger.LogInformation("Picking next modifiers with seed: {modSeed}", rand.seed);
        
        var modifiersAmount = 1 + rand.NextInt() % 3;
        var stateArtifacts = state.artifacts
            .Concat(state.characters.SelectMany(c => c.artifacts))
            .Select(artifact => artifact.GetType());
        List<Type> picked = [];
        foreach (var modifier in Archipelago.Instance.APSaveData.FoundModifiers
                     .ExceptBy(Archipelago.Instance.APSaveData.NextModifierRando, m => m.Name)
                     .Except(stateArtifacts)
                     .Shuffle(rand)
                     .Take(modifiersAmount))
        {
            if (!picked.Any(alreadyPicked => DailyDescriptor.AreDailyModifierArtifactsMutuallyExclusive(modifier.Name, alreadyPicked.Name)))
            {
                picked.Add(modifier);
                ModEntry.Instance.Logger.LogInformation("Adding modifier: {modifier}", modifier);
            }
        }
        if (picked.Contains(typeof(DailyDraftPick)) && picked.Contains(typeof(DailyBossArtifactTreat)))
        {
            picked.Remove(typeof(DailyDraftPick));
            picked.Add(typeof(DailyDraftPick));
        }

        Archipelago.Instance.APSaveData.NextModifierRando = picked.Select(m => m.Name).ToHashSet();
        APSaveData.Save();
    }
    
    private static Card? RandomOrNull(List<Card> list, Rand rng)
    {
        return list.Count == 0 ? null :
            list.Count < 2 ? list[0] :
            list[rng.NextInt() % list.Count];
    }

    private static bool IsBasic(Card c) =>
        c is CannonColorless or DodgeColorless or BasicShieldColorless or DroneshiftColorless;
}