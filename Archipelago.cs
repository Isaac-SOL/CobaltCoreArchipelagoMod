using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace CobaltCoreArchipelago;

public class Archipelago
{
    public static Archipelago Instance { get; private set; } = null!;

    public static Dictionary<string, Deck> ItemToDeck = new()
    {
        { "Dizzy", Deck.dizzy },
        { "Riggs", Deck.riggs },
        { "Peri",  Deck.peri },
        { "Isaac", Deck.goat },
        { "Drake", Deck.eunice },
        { "Max",   Deck.hacker },
        { "Books", Deck.shard },
        { "CAT",   Deck.colorless }
    };

    public static Dictionary<string, string> ItemToStartingShip = new()
    {
        { "Artemis",    "artemis" },
        { "Ares",       "ares" },
        { "Jupiter",    "jupiter" },
        { "Gemini",     "gemini" },
        { "Tiderunner", "boat" },
    };

    public static Dictionary<string, Type> ItemToCard = new()
    {
        // Dizzy Common
        { "Big Shield", typeof(BigShield) },
        { "Block Shot", typeof(BlockShot) },
        { "Boost Capacitors", typeof(BoostCapacitors) },
        { "Button Mash", typeof(ButtonMash) },
        { "Deflection", typeof(Deflection) },
        { "Momentum", typeof(MomentumCard) },
        { "Shield Surge", typeof(BlockParty) },
        { "Stun Charge", typeof(StunCharge) },
        { "Stun Shot", typeof(StunShot) },
        // Dizzy Uncommon
        { "Acid Cannon", typeof(AcidCannon) },
        { "Blocker Burnout", typeof(BlockerBurnout) },
        { "Converter", typeof(Converter) },
        { "EMP", typeof(EMPCard) },
        { "Multi Stun", typeof(DualStun) },
        { "Pulse Barrier", typeof(PulseBarrierCard) },
        { "Refresh Interval", typeof(RefreshInterval) },
        // Dizzy Rare
        { "Corrosion Beam", typeof(CorrosionBeam) },
        { "Mitosis", typeof(MitosisCard) },
        { "Payback", typeof(PaybackCard) },
        { "Shield Gun", typeof(ShieldGun) },
        { "Stun Source", typeof(StunSourceCard) },

        // Riggs Common
        { "Bolt", typeof(BoltCard) },
        { "Draw Shot", typeof(DrawCannon) },
        { "Evasive Shot", typeof(EvasiveShot) },
        { "Juke", typeof(Juke) },
        { "Options", typeof(OptionsCard) },
        { "Panic", typeof(Panic) },
        { "Quick Thinking", typeof(DrawThree) },
        { "Scramble", typeof(Scramble) },
        { "Whiplash", typeof(Whiplash) },
        // Riggs Uncommon
        { "Charge Beam", typeof(ChargeBeam) },
        { "Echo", typeof(EchoCard) },
        { "Fleetfoot", typeof(FleetfootCard) },
        { "Now Or Never", typeof(NowOrNeverCard) },
        { "Prepare", typeof(PrepareCard) },
        { "Selective Memory", typeof(SelectiveMemory) },
        { "Vamoose", typeof(Vamoose) },
        // Riggs Rare
        { "Ace", typeof(Ace) },
        { "Hand Cannon", typeof(HandCannon) },
        { "Second Opinions", typeof(SecondOpinions) },
        { "Strafe", typeof(Strafe) },
        { "Think Twice", typeof(ThinkTwice) },

        // Peri Common
        { "Escalate", typeof(Overdrive) },
        { "Extra Battery", typeof(SpareBattery) },
        { "Feint", typeof(Feint) },
        { "Lunge", typeof(Lunge) },
        { "Multi Blast", typeof(BigGun) },
        { "Multi Shot", typeof(MultiShot) },
        { "Overpower", typeof(Overpower) },
        { "Scoot", typeof(ScootRight) },
        { "Wave Charge", typeof(WaveCharge) },
        // Peri Uncommon
        { "Barrage", typeof(Barrage) },
        { "Battle Repair", typeof(BattleRepairs) },
        { "Flux", typeof(LibraCard) },
        { "Frontloaded Blast", typeof(FumeCannon) },
        { "Power Play", typeof(PowerdriveCard) },
        { "Rev the Engines", typeof(RevTheEngines) },
        { "Sidestep", typeof(Sidestep) },
        // Peri Rare
        { "Endless Magazine", typeof(EndlessMagazine) },
        { "Inverter", typeof(Inverter) },
        { "Parry", typeof(Parry) },
        { "Table Flip", typeof(TableFlipCard) },
        { "Weaken Hull", typeof(WeakenHull) },

        // Isaac Common
        { "Attack Drone", typeof(AttackDroneCard) },
        { "Flex Move", typeof(FlexMove) },
        { "Missile Shot", typeof(MissileLaunchCard) },
        { "Parallel Shift", typeof(DroneShiftCard) },
        { "Shield Drone", typeof(ShieldDroneCard) },
        { "Shift Shot", typeof(ShiftShot) },
        { "Small Boulder", typeof(SmallBoulder) },
        { "Solar Breeze", typeof(SolarBreeze) },
        { "Space Mine", typeof(SpaceMineCard) },
        // Isaac Uncommon
        { "Battalion", typeof(Battalion) },
        { "Boulder Bundle", typeof(BoulderPack) },
        { "Bubble Field", typeof(BubbleField) },
        { "Large Boulders", typeof(LargeBoulder) },
        { "Radio Control", typeof(DroneTurnCard) },
        { "Repair Kit", typeof(RepairKitCard) },
        { "Striker Squadron", typeof(StrikerSquadron) },
        // Isaac Rare
        { "Bay Overload", typeof(BayOverload) },
        { "Energy Drone", typeof(EnergyDroneCard) },
        { "Jupiter Drone", typeof(MiniMe) },
        { "Rock Factory", typeof(RockFactory) },
        { "Scattershot", typeof(ScatterShot) },

        // Drake Common
        { "Combustion Engine", typeof(CombustionEngine) },
        { "Desperate Measures", typeof(DesperateMeasures) },
        { "EMP Slug", typeof(EMPSlug) },
        { "Exothermic Release", typeof(ExothermicRelease) },
        { "Explosive Slug", typeof(HESlug) },
        { "Firewall", typeof(Firewall) },
        { "Heatsink", typeof(HeatSink) },
        { "Hot Compress", typeof(HotCompress) },
        { "Hotfoot", typeof(Hotfoot) },
        // Drake Uncommon
        { "Aggressive Armoring", typeof(AggressiveArmoring) },
        { "Flash Point", typeof(FlashPoint) },
        { "Heatwave", typeof(Heatwave) },
        { "Sear", typeof(SearCard) },
        { "Solar Flair", typeof(SolarFlair) },
        { "Ventilator", typeof(Ventilator) },
        { "Volatile Vapor", typeof(VolatileVaporCard) },
        // Drake Rare
        { "Freeze Dry", typeof(FreezeDry) },
        { "From Hell's Heart", typeof(HellsHeartCard) },
        { "Pillage and Plunder", typeof(HealthCannon) },
        { "Serenity", typeof(Serenity) },
        { "Thermal Battery", typeof(ThermalBattery) },

        // Max Common
        { "Admin Deploy", typeof(AdminDeployCard) },
        { "Cloud Save", typeof(CloudSaveCard) },
        { "Dice Roll", typeof(DiceRoll) },
        { "Math.Max", typeof(MathMaxCard) },
        { "Reroll", typeof(RerollCard) },
        { "Reroute", typeof(RerouteCard) },
        { "Shuffle Shot", typeof(ShuffleShot) },
        { "System Security", typeof(SystemSecurity) },
        { "Worm", typeof(WormFood) },
        // Max Uncommon
        { "Branch Prediction", typeof(BranchPrediction) },
        { "Enrage", typeof(Enrage) },
        { "Escape Artist", typeof(EscapeArtist) },
        { "Lazy Barrage", typeof(LazyBarrage) },
        { "Memory Leak", typeof(MaxTrashGeneration) },
        { "Root Access", typeof(RootAccess) },
        { "Spacer", typeof(Spacer) },
        // Max Rare
        { "Backup Stick", typeof(BackupStick) },
        { "Clean Exhaust", typeof(CleanExhaustCard) },
        { "Overclock", typeof(Overclock) },
        { "Save State", typeof(SaveStateCard) },
        { "Total Cache Wipe", typeof(ExhaustHandCard) },

        // Books Common
        { "Glimmer Shot", typeof(Glimmershot) },
        { "Mage Hand", typeof(MageHand) },
        { "Magi-Battery", typeof(MagiBattery) },
        { "Meteor", typeof(MeteorCard) },
        { "Mining Drill", typeof(MiningDrillCard) },
        { "Sapphire Shield", typeof(Shardsource) },
        { "Swizzle Shift", typeof(SwizzleShift) },
        { "Unpolished Crystal", typeof(UnpoweredShardCard) },
        { "Zircon Zip", typeof(ZirconZip) },
        // Books Uncommon
        { "Avid Reader", typeof(AvidReader) },
        { "Block Evolution", typeof(BlockEvolution) },
        { "Bloodstone Bolt", typeof(BloodstoneBolt) },
        { "Catch", typeof(CatchCard) },
        { "Mineral Deposit", typeof(MineralDeposit) },
        { "Ol' Reliable", typeof(GeodeCard) },
        { "Shardpack", typeof(ShardPack) },
        // Books Rare
        { "Medusa Field", typeof(MedusaField) },
        { "Overflowing Power", typeof(OverflowingPower) },
        { "Perfect Specimen", typeof(PerfectSpecimen) },
        { "Quantum Quarry", typeof(QuantumQuarryCard) },
        { "Zero Draw", typeof(ZeroDraw) },

        // CAT Common
        { "Defensive Mode", typeof(DefensiveMode) },
        { "Dizzy.EXE", typeof(ColorlessDizzySummon) },
        { "Drake.EXE", typeof(ColorlessDrakeSummon) },
        { "Isaac.EXE", typeof(ColorlessIsaacSummon) },
        { "Max.EXE", typeof(ColorlessMaxSummon) },
        { "Peri.EXE", typeof(ColorlessPeriSummon) },
        { "Riggs.EXE", typeof(ColorlessRiggsSummon) },
        // CAT Uncommon
        { "Aegis", typeof(AegisCard) },
        { "Books.EXE", typeof(ColorlessBooksSummon) },
        { "CAT.EXE", typeof(ColorlessCATSummon) },
        { "I Frames", typeof(IFrameCard) },
        { "Jack of All Trades", typeof(JackOfAllTrades) },
        { "Quick Fix", typeof(CATTempFixCard) },
        { "Temporal Anomaly", typeof(TemporalAnomalyCard) },
        // CAT Rare
        { "Adaptability", typeof(AdaptabilityCard) },
        { "AI Overflow", typeof(AIOverflowCard) },
        { "Prism", typeof(PrismAttackCard) },
        { "Time Skip", typeof(TimestopCard) }
    };

    public static Dictionary<string, Type> ItemToArtifact = new()
    {
        // Basic Common
        { "Nanofiber Hull",  typeof(NanofiberHull) },
        { "Overcharger", typeof(Overcharger) },
        { "Crosslink", typeof(Crosslink) },
        { "Shield Memory", typeof(ShieldMemory) },
        { "Jumper Cables", typeof(JumperCables) },
        { "Hull Plating", typeof(HullPlating) },
        { "Armored Bay", typeof(ArmoredBay) },
        { "Recalibrator", typeof(Recalibrator) },
        { "Grazer Beam", typeof(GrazerBeam) },
        { "Jet Thrusters", typeof(JetThrusters) },
        { "Prepped Batteries", typeof(EnergyPrep) },
        { "Energy Refund", typeof(EnergyRefund) },
        { "Overclocked Generator", typeof(OverclockedGenerator) },
        { "Cockpit Lock-On", typeof(CockpitTarget) },
        { "Stun Calibrator", typeof(StunCalibrator) },
        { "Ion Converter", typeof(IonConverter) },
        { "Fracture Detection", typeof(FractureDetection) },
        { "Ricochet Paddle", typeof(RicochetPaddle) },
        { "Adaptive Plating", typeof(AdaptivePlating) },
        { "Piercer", typeof(Piercer) },
        { "Heal Booster", typeof(HealBooster) },
        { "Jettison Hatch", typeof(JettisonHatch) },
        { "Sharp Edges", typeof(SharpEdges) },
        { "Chaff Emitters", typeof(ChaffEmitters) },
        // Ship-specific Common
        { "Radar Subwoofer", typeof(RadarSubwoofer) },
        // Basic Boss
        { "Glass Cannon", typeof(GlassCannon) },
        { "Simplicity", typeof(Simplicity) },
        { "Dirty Engines", typeof(DirtyEngines) },
        { "Genesis", typeof(Genesis) },
        { "Hi Freq Intercom", typeof(HiFreqIntercom) },
        // Ship-specific Boss
        { "Warp Mastery", typeof(WarpMastery) },
        { "Hunter Wings", typeof(HunterWings) },
        { "Ares Cannon V2", typeof(AresCannonV2) },
        { "Jupiter Drone Hub Booster", typeof(JupiterDroneHubV2) },
        { "Gemini Core Booster", typeof(GeminiCoreBooster) },
        { "Mooring Line V2", typeof(TideRunnerAnchorV2)},
        
        // Dizzy
        { "Photon Condenser", typeof(DizzyBoost) },
        { "Shield Reserves", typeof(ShieldReserves) },
        { "Rebound Reagent", typeof(ReboundReagent) },
        { "Regenerator", typeof(Regenerator) },
        // Dizzy Boss
        { "Shield Burst", typeof(ShieldBurst) },
        { "Prototype 22", typeof(Prototype22) },

        // Riggs
        { "Quickdraw", typeof(Quickdraw) },
        { "Perpetual Motion Device", typeof(PerpetualMotionDevice) },
        { "Caffeine Rush", typeof(CaffeineRush) },
        // Riggs Boss
        { "Demon Thrusters", typeof(DemonThrusters) },
        { "Flywheel", typeof(Flywheel) },

        // Peri
        { "Dakka Drum", typeof(DakkaDrum) },
        { "Revenge Drive", typeof(RevengeDrive) },
        { "Premeditation", typeof(Premeditation) },
        // Peri Boss
        { "Power Diversion", typeof(PowerDiversion) },
        { "Berserker Drive", typeof(BerserkerDrive) },

        // Isaac
        { "Wave Control", typeof(GoatThrusters) },
        { "Bubbler", typeof(Bubbler) },
        { "Gravel Recycler", typeof(GravelRecycler) },
        { "Drone Piercer", typeof(DronePiercer) },
        // Isaac Boss
        { "Radio Repeater", typeof(RadioRepeater) },
        { "Salvage Arm", typeof(SalvageArm) },

        // Drake
        { "Pressure Fuse", typeof(PressureFuse) },
        { "Subzero Heatsinks", typeof(SubzeroHeatsinks) },
        { "Ignition Coil", typeof(IgnitionCoil) },
        { "Heat Distiller", typeof(HeatDistiller) },
        { "Next Gen Insulation", typeof(NextGenInsulation) },
        // Drake Boss
        { "Thermo Reactor", typeof(ThermoReactor) },

        // Max
        { "Safety Lock", typeof(SafetyLock) },
        { "Sticky Note", typeof(StickyNote) },
        { "Right Click", typeof(RightClickArtifact) },
        { "Strong Start", typeof(StrongStart) },
        // Max Boss
        { "Flow State", typeof(FlowState) },
        { "Tridimensional Cockpit", typeof(TridimensionalCockpit) },
        { "Lightspeed Boot Disk", typeof(LightspeedBootDisk) },

        // Books
        { "Grimoire", typeof(Grimoire) },
        { "Resonance Fork", typeof(ResonanceFork) },
        { "Shard Enchanter", typeof(ShardEnchanter) },
        { "Shard Collector", typeof(ShardCollector) },
        { "Rock Collection", typeof(RockCollection) },
        // Books Boss
        { "Zero Doubler", typeof(ZeroDoubler) },

        // CAT
        { "Standby Mode", typeof(StandbyMode) },
        { "Initial Booster", typeof(InitialBooster) },
        { "Multi Threading", typeof(MultiThreading) },
        // CAT Boss
        { "Summon Control", typeof(SummonControl) },
    };
    
    public ArchipelagoSession? Session { get; set; }

    public Dictionary<string, object>? SlotData { get; set; }
    
    public SlotDataHelper? SlotDataHelper { get; set; }

    public static SlotDataHelper InstanceSlotData => Instance.SlotDataHelper!.Value;

    public ILogger Logger => ModEntry.Instance.Logger;

    public Archipelago()
    {
        Instance = this;
    }

    public void Connect(string hostname, int port, string slot)
    {
        Session = ArchipelagoSessionFactory.CreateSession(hostname, port);
        var loginResult =
            Session.TryConnectAndLogin("Cobalt Core", slot, ItemsHandlingFlags.AllItems);
        if (!loginResult.Successful)
        {
            Logger.LogError("Failed to connect to Archipelago host");
            throw new Exception("Archipelago connection error");
        }

        SlotData = (loginResult as LoginSuccessful)!.SlotData;
        Logger.LogInformation("Successfully connected to Archipelago host");
        Logger.LogInformation("Slot Information:");
        foreach (var kvp in SlotData)
        {
            Logger.LogInformation("{key} : {value}", kvp.Key, kvp.Value);
        }

        SlotDataHelper = CobaltCoreArchipelago.SlotDataHelper.FromSlotData(SlotData);
        
        // Patch starting decks
        foreach (var deck in ItemToDeck.Values)
        {
            StarterDeck.starterSets[deck].cards = new List<Card>();
            foreach (var card in SlotDataHelper.Value.DeckStartingCards[deck])
            {
                StarterDeck.starterSets[deck].cards.Add((Card)card.CreateInstance());
            }
        }
        // Patch starting ships
        foreach (var shipName in StarterShip.ships.Keys)
        {
            var shuffledParts = ModEntry.BaseShips[shipName].ship.parts
                .Shuffle(new Rand(SlotDataHelper.Value.FixedRandSeed));
            StarterShip.ships[shipName].ship.parts = Mutil.DeepCopy(new List<Part>(shuffledParts));
        }
        
        Session.Items.ItemReceived += helper => OnItemReceived(helper);
    }
    
    public void Disconnect()
    {
        if (Session == null) return;
        
        var task = Session.Socket.DisconnectAsync();
        
        try
        {
            task.Wait(TimeSpan.FromSeconds(5));
            Logger.LogInformation("Successfully disconnected from Archipelago host");
        }
        catch (AggregateException e)
        {
            Logger.LogWarning(e, "Error when disconnecting from Archipelago host");
        }
        
        Session = null;
        SlotData = null;
        SlotDataHelper = null;
    }

    public void Reconnect(string hostname, int port, string slot)
    {
        Disconnect();
        Connect(hostname, port, slot);
    }

    private void OnItemReceived(ReceivedItemsHelper helper, bool animate = true)
    {
        var item = helper.PeekItem();
        var name = item.ItemName;
        Logger.LogInformation("Received {item}", name);
        if (ItemToStartingShip.ContainsKey(name))
        {
            
        }
        else if (ItemToDeck.ContainsKey(name))
        {
            
        }
        else if (ItemToCard.ContainsKey(name))
        {
            
        }
        else if (ItemToArtifact.ContainsKey(name))
        {
            
        }
    }
}

public enum WinCondition
{
    TotalMemories = 0,
    MemoryPerCharacter
}

public enum CardRewardsMode
{
    Never = 0,
    IfHasDeck,
    IfLocal,
    IfLocalAndHasDeck,
    Always
}

public class SlotDataInvalidException(string message) : Exception(message);

public struct SlotDataHelper
{
    public List<Deck> StartingCharacters { get; set; }
    public StarterShip StartingShip { get; set; }
    public List<Type> StartingCards { get; set; }
    public Dictionary<Deck, List<Type>> DeckStartingCards { get; set; }
    public NewRunOptions.DifficultyLevel MinimumDifficulty { get; set; }
    public WinCondition WinCondition { get; set; }
    public int WinReqTotal { get; set; }
    public int WinReqPerChar { get; set; }
    public bool AddCharacterMemories { get; set; }
    public bool DoFutureMemory { get; set; }
    public CardRewardsMode ImmediateCardRewards { get; set; }
    public uint FixedRandSeed { get; set; }

    public static SlotDataHelper FromSlotData(Dictionary<string, object> slotData)
    {
        var res = new SlotDataHelper();
        try
        {
            var startingCharacters = (JArray)slotData["starting_characters"];
            res.StartingCharacters = [];
            res.StartingCharacters.AddRange(startingCharacters.Select(s => Archipelago.ItemToDeck[s.ToString()]));
            var startingShip = Archipelago.ItemToStartingShip[(string)slotData["starting_ship"]];
            res.StartingShip = StarterShip.ships[startingShip];
            var startingCards = (JArray)slotData["starting_cards"];
            res.StartingCards = [];
            res.StartingCards.AddRange(startingCards.Select(s => Archipelago.ItemToCard[s.ToString()]));
            res.DeckStartingCards = new Dictionary<Deck, List<Type>>();
            foreach (var card in res.StartingCards)
            {
                var deck = ((CardMeta)Attribute.GetCustomAttribute(card, typeof(CardMeta))!).deck;
                if (!res.DeckStartingCards.ContainsKey(deck))
                    res.DeckStartingCards[deck] = new List<Type> { card };
                else
                    res.DeckStartingCards[deck].Add(card);
            }
            res.MinimumDifficulty = NewRunOptions.difficulties[Convert.ToInt32(slotData["minimum_difficulty"])];
            res.WinCondition = (WinCondition)Convert.ToInt32(slotData["win_condition"]);
            res.WinReqTotal = Convert.ToInt32(slotData["memories_required_total"]);
            res.WinReqPerChar = Convert.ToInt32(slotData["memories_required_per_character"]);
            res.DoFutureMemory = Convert.ToBoolean(slotData["do_future_memory"]);
            res.AddCharacterMemories = Convert.ToBoolean(slotData["add_character_memories"]);
            res.ImmediateCardRewards = (CardRewardsMode)Convert.ToInt32(slotData["immediate_card_rewards"]);
            res.FixedRandSeed = 5318008; // TODO define that server-side
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new SlotDataInvalidException(e.Message);
        }

        return res;
    }
}
