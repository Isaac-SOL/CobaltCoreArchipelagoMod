using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;
using CobaltCoreArchipelago.Features;
using CobaltCoreArchipelago.GameplayPatches;
using CobaltCoreArchipelago.MenuPatches;
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

    public static Dictionary<string, Deck> ItemToMemory = ItemToDeck
        .Select(pair => (pair.Key + " Memory", pair.Value))
        .ToDictionary();

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
        // CAT Uncommon
        { "Aegis", typeof(AegisCard) },
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

    public static Dictionary<Type, string> CardToItem = ItemToCard
        .Select(pair => (pair.Value, pair.Key))
        .ToDictionary();

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

    public static Dictionary<Type, string> ArtifactToItem = ItemToArtifact
        .Select(pair => (pair.Value, pair.Key))
        .ToDictionary();
    
    public APSaveData? APSaveData { get; set; }
    public ArchipelagoSession? Session { get; set; }
    public Dictionary<string, object>? SlotData { get; set; }
    public SlotDataHelper? SlotDataHelper { get; set; }
    public static SlotDataHelper InstanceSlotData => Instance.SlotDataHelper!.Value;
    public ILogger Logger => ModEntry.Instance.Logger;
    public bool Ready { get; private set; } = false;
    public DeathLinkService? DeathLinkService { get; set; }

    private static ConcurrentBag<(string name, string sender)> receivedItemsToProcess = [];
    private static readonly object itemReceivedLock = new();
    private static DeathLink? lastDeathLink;
    private static readonly object deathLinkLock = new();
    public List<string> MessagesReceived { get; } = [];
    public List<(string message, Color color)[]> MessagePartsReceived { get; } = [];
    private static readonly object messagesReceivedLock = new();

    public Archipelago()
    {
        Instance = this;
    }

    public void LoadSaveData(int slot)
    {
        APSaveData = APSaveData.LoadFromSlot(slot);
    }

    public (LoginResult, ArchipelagoErrorCode) Connect()
    {
        Debug.Assert(APSaveData != null, nameof(APSaveData) + " != null");
        Debug.Assert(Session == null, nameof(Session) + " == null");
        Session = ArchipelagoSessionFactory.CreateSession(APSaveData.Hostname, APSaveData.Port);
        var loginResult =
            Session.TryConnectAndLogin(
                "Cobalt Core",
                APSaveData.Slot,
                ItemsHandlingFlags.AllItems,
                password: APSaveData.Password);
        if (!loginResult.Successful)
        {
            Logger.LogError("Failed to connect to Archipelago host");
            return (loginResult, ArchipelagoErrorCode.ConnectionIssue);
        }

        var code = ArchipelagoErrorCode.Ok;

        SlotData = (loginResult as LoginSuccessful)!.SlotData;
        Logger.LogInformation("Successfully connected to Archipelago host");
        Logger.LogInformation("Slot Information:");
        foreach (var kvp in SlotData)
        {
            Logger.LogInformation("{key} : {value}", kvp.Key, kvp.Value);
        }

        if (APSaveData.RoomId is not null && APSaveData.RoomId != Session.RoomState.Seed)
        {
            Logger.LogError("Stored seed is different from Archipelago host seed");
            code = ArchipelagoErrorCode.RoomIdConflict;
        }
        
        return (loginResult, code);
    }

    internal void ApplyArchipelagoConnection()
    {
        Debug.Assert(SlotData != null, nameof(SlotData) + " != null");
        Debug.Assert(Session != null, nameof(Session) + " != null");
        Debug.Assert(APSaveData != null, nameof(APSaveData) + " != null");
        SlotDataHelper = CobaltCoreArchipelago.SlotDataHelper.FromSlotData(SlotData);
        APSaveData.RoomId = Session.RoomState.Seed;
        MessagesReceived.Clear();
        MessagePartsReceived.Clear();
        MessagePartsReceived.Add([
            (ModEntry.Instance.Localizations.Localize(["mainMenu", "welcomeMessage"]), Colors.white)
        ]);
        
        // Patch starting decks
        foreach (var deck in ItemToDeck.Values)
        {
            StarterDeck.starterSets[deck].cards = [];
            foreach (var card in SlotDataHelper.Value.DeckStartingCards[deck])
            {
                StarterDeck.starterSets[deck].cards.Add((Card)card.CreateInstance());
            }
        }
        // Patch starting ships
        if (SlotDataHelper.Value.ShuffleShipParts)
        {
            foreach (var shipName in StarterShip.ships.Keys)
            {
                var shuffledParts = ModEntry.BaseShips[shipName].ship.parts
                    .Shuffle(new Rand(SlotDataHelper.Value.FixedRandSeed));
                StarterShip.ships[shipName].ship.parts = Mutil.DeepCopy(new List<Part>(shuffledParts));
            }
        }
        // Patch memories
        Vault.charsWithLore = Mutil.DeepCopy(ModEntry.BaseCharsWithLore);
        if (SlotDataHelper.Value.AddCharacterMemories)
        {
            Vault.charsWithLore.Add(Deck.shard);
            Vault.charsWithLore.Add(Deck.colorless);
        }

        Ready = true;
        APSaveData.SyncWithHost();  // Consumes items queue
        
        Session.Items.ItemReceived += OnItemReceived;
        Session.MessageLog.OnMessageReceived += OnMessageReceived;

        DeathLinkService = Session.CreateDeathLinkService();
        DeathLinkService.OnDeathLinkReceived += OnDeathLinkReceived;
        if (APSaveData.DeathLinkMode != DeathLinkMode.Off)
            DeathLinkService.EnableDeathLink();
    }

    public void Disconnect()
    {
        if (Session == null) return;
        Ready = false;
        
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

    public (LoginResult, ArchipelagoErrorCode) Reconnect()
    {
        Disconnect();
        return Connect();
    }

    public void CheckLocationsForced(params long[] address)
    {
        Debug.Assert(Session != null, nameof(Session) + " != null");
        _ = Task.Run(() =>
        {
            var checkLocationTask = Session.Locations.CompleteLocationChecksAsync(address);
            if (!checkLocationTask.Wait(TimeSpan.FromSeconds(2)))
            {
                Logger.LogWarning("Failed to check location {location} on the Archipelago host", address);
            }
        });
    }

    public void CheckLocation(string name)
    {
        Debug.Assert(Session != null, nameof(Session) + " != null");
        Debug.Assert(APSaveData != null, nameof(APSaveData) + " != null");
        if (APSaveData.LocationsChecked.Contains(name))
            return;
        CheckLocationsForced(Session.Locations.GetLocationIdFromName("Cobalt Core", name));
        APSaveData.AddCheckedLocation(name);
    }

    private void OnItemReceived(ReceivedItemsHelper helper)
    {
        lock (itemReceivedLock)  // TODO is that lock redundant with the ConcurrentBag?
        {
            while (helper.PeekItem() != null)
            {
                var name = helper.PeekItem().ItemName;
                var sender = helper.PeekItem().Player.Name;
                // We are currently on the websocket thread.
                // To prevent concurrency issues we store received items in a thread-safe list to process on the main thread.
                receivedItemsToProcess.Add((name, sender));
                helper.DequeueItem();
            }
        }
    }

    private void OnMessageReceived(LogMessage message)
    {
        Logger.LogInformation("Received message: {message}", message);
        var sb = new StringBuilder();
        var parts = new List<(string message, Color color)>();
        foreach (var part in message.Parts)
        {
            var colorString = part.Color.R.ToString("X2")
                              + part.Color.G.ToString("X2")
                              + part.Color.B.ToString("X2");
            sb.Append("<c=");
            sb.Append(colorString);
            sb.Append('>');
            sb.Append(part.Text);
            sb.Append("</c>");
            var thisColor = new Color(colorString);
            parts.Add((part.Text, thisColor));
        }

        lock (messagesReceivedLock)
        {
            MessagesReceived.Add(sb.ToString());
            MessagePartsReceived.Add(parts.ToArray());
            if (MainMenuPatch.messagesPos != 0) MainMenuPatch.messagesPos++;
        }
    }

    private void OnDeathLinkReceived(DeathLink deathLink)
    {
        lock (deathLinkLock)
        {
            lastDeathLink = deathLink;
        }
    }

    internal void SafeUpdate(G g)
    {
        Debug.Assert(APSaveData != null, nameof(APSaveData) + " != null");
        
        var state = g.state;
        lock (itemReceivedLock)
        {
            ItemApplier.ApplyDeferredItems(state);
            while (receivedItemsToProcess.TryTake(out var item))
            {
                Logger.LogInformation("Received {item} from {player}", item.name, item.sender);
                ItemApplier.ApplyReceivedItem(item, state);
            }
        }

        lock (deathLinkLock)
        {
            if (lastDeathLink is not null && state.storyVars.hasStartedGame && state.ship.hull > 0)
            {
                DeathLinkManager.ApplyDeathLink(g, lastDeathLink);
            }

            lastDeathLink = null;
        }
    }

    internal async Task<ScoutedItemInfo?[]> ScoutLocationInfo(params string[] locationNames)
    {
        Debug.Assert(Session != null, nameof(Session) + " != null");
        Debug.Assert(APSaveData != null, nameof(APSaveData) + " != null");
        var addresses = locationNames.Select(s => Session.Locations.GetLocationIdFromName("Cobalt Core", s)).ToArray();
        var info = await Session.Locations.ScoutLocationsAsync(
            APSaveData.CardScoutMode == CardScoutMode.CreateHint
                ? HintCreationPolicy.CreateAndAnnounceOnce
                : HintCreationPolicy.None,
            addresses
        );
        var res = addresses.Select(a => info.GetValueOrDefault(a));
        return res.ToArray();
    }
}

public static class APColors
{
    internal const string OtherPlayer = "FFFF00";
    internal const string Self = "FF00FF";
    internal const string Location = "008000";
    internal const string Filler = "00FFFF";
    internal const string Useful = "6A5ACD";
    internal const string Progression = "DDA0DD";
    internal const string Trap = "FF0000";

    internal static string GetColor(this ItemInfo item) =>
        (item.Flags & ItemFlags.Advancement) != 0 ? Progression :
        (item.Flags & ItemFlags.Trap) != 0 ? Trap :
        (item.Flags & ItemFlags.NeverExclude) != 0 ? Useful :
        Filler;
}

public enum ArchipelagoErrorCode
{
    Ok = 0,
    ConnectionIssue,
    RoomIdConflict
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

[Flags]
public enum CardRewardAttribute
{
    None = 0,
    Temporary = 1,
    SingleUse = 2,
    Exhaust = 4,
    Discount = 8,
    Recycle = 16,
    Retain = 32
}


public class SlotDataInvalidException(string message) : Exception(message);

public struct SlotDataHelper
{
    public List<Deck> StartingCharacters { get; private set; }
    public string StartingShip { get; private set; }
    public bool ShuffleShipParts { get; private set; }
    public List<Type> StartingCards { get; private set; }
    public Dictionary<Deck, List<Type>> DeckStartingCards { get; private set; }
    public WinCondition WinCondition { get; private set; }
    public int WinReqTotal { get; private set; }
    public int WinReqPerChar { get; private set; }
    public bool AddCharacterMemories { get; private set; }
    public bool ShuffleMemories { get; private set; }
    public bool DoFutureMemory { get; private set; }
    public bool ShuffleCards { get; private set; }
    public bool ShuffleArtifacts { get; private set; }
    public int CheckCardDifficulty { get; private set; }
    public bool RarerChecksLater { get; private set; }
    public bool GetMoreFoundItems { get; private set; }
    public CardRewardsMode ImmediateCardRewards { get; private set; }
    public CardRewardAttribute ImmediateCardAttribute { get; private set; }
    public CardRewardsMode ImmediateArtifactRewards { get; private set; }
    public uint FixedRandSeed { get; private set; }

    public bool HasImmediateCardAttribute(CardRewardAttribute attribute)
        => (ImmediateCardAttribute & attribute) != CardRewardAttribute.None;

    public static SlotDataHelper FromSlotData(Dictionary<string, object> slotData)
    {
        var res = new SlotDataHelper();
        try
        {
            var startingCharacters = (JArray)slotData["starting_characters"];
            res.StartingCharacters = [];
            res.StartingCharacters.AddRange(startingCharacters.Select(s => Archipelago.ItemToDeck[s.ToString()]));
            res.StartingShip = Archipelago.ItemToStartingShip[(string)slotData["starting_ship"]];
            res.ShuffleShipParts = Convert.ToBoolean(slotData["shuffle_ship_parts"]);
            var startingCards = (JArray)slotData["starting_cards"];
            res.StartingCards = [];
            res.StartingCards.AddRange(startingCards.Select(s => Archipelago.ItemToCard[s.ToString()]));
            res.DeckStartingCards = new Dictionary<Deck, List<Type>>();
            foreach (var deck in Archipelago.ItemToDeck.Values)
            {
                res.DeckStartingCards[deck] = [];
            }
            foreach (var card in res.StartingCards)
            {
                var deck = ((CardMeta)Attribute.GetCustomAttribute(card, typeof(CardMeta))!).deck;
                res.DeckStartingCards[deck].Add(card);
            }
            res.WinCondition = (WinCondition)Convert.ToInt32(slotData["win_condition"]);
            res.WinReqTotal = Convert.ToInt32(slotData["memories_required_total"]);
            res.WinReqPerChar = Convert.ToInt32(slotData["memories_required_per_character"]);
            res.ShuffleMemories = Convert.ToBoolean(slotData["shuffle_memories"]);
            res.DoFutureMemory = Convert.ToBoolean(slotData["do_future_memory"]);
            res.ShuffleCards = Convert.ToBoolean(slotData["shuffle_cards"]);
            res.ShuffleArtifacts = Convert.ToBoolean(slotData["shuffle_artifacts"]);
            res.CheckCardDifficulty = Convert.ToInt32(slotData["check_card_difficulty"]);
            res.RarerChecksLater = Convert.ToBoolean(slotData["rarer_checks_later"]);
            res.AddCharacterMemories = Convert.ToBoolean(slotData["add_character_memories"]);
            res.GetMoreFoundItems = Convert.ToBoolean(slotData["get_more_found_items"]);
            res.ImmediateCardRewards = (CardRewardsMode)Convert.ToInt32(slotData["immediate_card_rewards"]);
            var attributes = (JArray)slotData["immediate_card_attributes"];
            foreach (var attribute in attributes)
            {
                res.ImmediateCardAttribute |= attribute.ToString() switch
                {
                    "Temporary" => CardRewardAttribute.Temporary,
                    "Single Use" => CardRewardAttribute.SingleUse,
                    "Exhaust" => CardRewardAttribute.Exhaust,
                    "Discount" => CardRewardAttribute.Discount,
                    "Recycle" => CardRewardAttribute.Recycle,
                    "Retain" => CardRewardAttribute.Retain,
                    _ => CardRewardAttribute.None
                };
            }
            res.ImmediateArtifactRewards = (CardRewardsMode)Convert.ToInt32(slotData["immediate_artifact_rewards"]);
            res.FixedRandSeed = Convert.ToUInt32(slotData["fixed_client_seed"]);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new SlotDataInvalidException(e.Message);
        }

        return res;
    }
}
