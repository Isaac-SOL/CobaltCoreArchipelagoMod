using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Archipelago.MultiClient.Net.Helpers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Nickel;
using Nickel.ModSettings;

namespace CobaltCoreArchipelago;

public class APSaveData
{
    [JsonIgnore]
    internal static Dictionary<int, APSaveData> AllAPSaves
    {
        get
        {
            if (_allApSaveStorage is null)
                LoadAllSaves();
            return _allApSaveStorage!;
        }
    }
    [JsonIgnore]
    private static Dictionary<int, APSaveData>? _allApSaveStorage;
    internal static IModStorage ModStorage => ModEntry.Instance.Helper.Storage;
    
    // General AP info
    [JsonProperty]
    internal int SaveSlot { get; }
    [JsonProperty]
    internal string Hostname { get; set; }
    [JsonProperty]
    internal int Port { get; set; }
    [JsonProperty]
    internal string Slot { get; set; }
    [JsonProperty]
    internal string? Password { get; set; }
    [JsonProperty]
    internal string? RoomId { get; set; }
    [JsonProperty]
    internal Dictionary<string, int> AppliedInventory { get; }
    [JsonProperty]
    internal HashSet<string> LocationsChecked { get; }
    [JsonProperty]
    internal int LastCombatCount { get; set; }
    [JsonProperty]
    internal int DeathLinkCount { get; set; }
    [JsonProperty]
    internal HashSet<string> RecentlySeenLocations { get; set; }
    [JsonProperty]
    internal HashSet<string> AllSeenLocations { get; set; }
    [JsonProperty]
    internal HashSet<string> ThisRunSeenLocations { get; set; }
    [JsonProperty]
    internal Dictionary<string, List<int>> NextShipRando { get; set; }
    [JsonProperty]
    internal Dictionary<Deck, List<string>> NextCardRando { get; set; }
    [JsonProperty]
    internal HashSet<string> NextModifierRando { get; set; }
    [JsonProperty]
    internal HashSet<string> PeopleWeWronged { get; set; }
    
    // Mod settings
    [JsonProperty]
    internal DeathLinkMode DeathLinkMode { get; set; } = DeathLinkMode.Off;
    [JsonProperty]
    internal int DeathLinkHullDamage { get; set; } = 4;
    [JsonProperty]
    internal int DeathLinkHullDamagePercent { get; set; } = 25;
    [JsonProperty]
    internal CardScoutMode CardScoutMode { get; set; } = CardScoutMode.ScoutOnly;
    [JsonProperty]
    internal bool MessagesInMenu { get; set; } = true;

    [JsonIgnore]
    internal IEnumerable<string> AppliedInventoryPositive => AppliedInventory
        .Where(kvp => kvp.Value > 0)
        .Select(kvp => kvp.Key);
    [JsonIgnore]
    internal IEnumerable<string> FoundShips => AppliedInventoryPositive
        .Intersect(Archipelago.ItemToStartingShip.Keys)
        .Select(s => Archipelago.ItemToStartingShip[s]);
    [JsonIgnore]
    internal IEnumerable<Deck> FoundChars => AppliedInventoryPositive
        .Intersect(Archipelago.ItemToDeck.Keys)
        .Select(s => Archipelago.ItemToDeck[s]);
    [JsonIgnore]
    internal IEnumerable<Type> FoundCards => AppliedInventoryPositive
        .Intersect(Archipelago.ItemToCard.Keys)
        .Select(s => Archipelago.ItemToCard[s]);
    [JsonIgnore]
    internal IEnumerable<Type> FoundArtifacts => AppliedInventoryPositive
        .Intersect(Archipelago.ItemToArtifact.Keys)
        .Select(s => Archipelago.ItemToArtifact[s]);
    [JsonIgnore]
    internal IEnumerable<Type> FoundModifiers => AppliedInventoryPositive
        .Intersect(Archipelago.ItemToModifier.Keys)
        .Select(s => Archipelago.ItemToModifier[s]);

    [JsonConstructor]
    private APSaveData(): this(0, "archipelago.gg", 38281, "CAT1")
    {
        
    }
    
    internal APSaveData(int saveSlot, string hostname, int port, string slot, string? password = null)
    {
        SaveSlot = saveSlot;
        Hostname = hostname;
        Port = port;
        Slot = slot;
        Password = password;
        AppliedInventory = new Dictionary<string, int>();
        LocationsChecked = [];
        RecentlySeenLocations = [];
        AllSeenLocations = [];
        ThisRunSeenLocations = [];
        NextShipRando = [];
        NextCardRando = [];
        NextModifierRando = [];
        PeopleWeWronged = [];
    }

    internal static void LoadAllSaves()
    {
        ModEntry.Instance.Logger.LogInformation("Storage File: {modStorage}",
                                                ModStorage.GetMainStorageFile("json").FullName);
        if (!ModStorage.TryLoadJson(ModStorage.GetMainStorageFile("json"), out _allApSaveStorage)
            || _allApSaveStorage is null)
        {
            ModEntry.Instance.Logger.LogWarning("Couldn't load mod storage json, creating new one");
            _allApSaveStorage = new Dictionary<int, APSaveData>();
            Save();
        }
    }

    internal static APSaveData LoadFromSlot(int saveSlot)
    {
        if (!AllAPSaves.TryGetValue(saveSlot, out APSaveData? value))
        {
            ModEntry.Instance.Logger.LogWarning("Couldn't find save data, creating new one");
            value = new APSaveData(saveSlot, "archipelago.gg", 38281, "CAT1");
            AllAPSaves[saveSlot] = value;
        }
        return value;
    }

    internal static void Save()
    {
        ModStorage.SaveJson(ModStorage.GetMainStorageFile("json"), AllAPSaves);
    }

    internal static void Erase(int slot)
    {
        ModEntry.Instance.Logger.LogInformation("Erasing AP data in slot {slot}", slot);
        AllAPSaves.Remove(slot);
        Save();
    }

    private static IReceivedItemsHelper APItems => Archipelago.Instance.Session!.Items;

    internal void SyncWithHost()
    {
        Debug.Assert(Archipelago.Instance.Session != null, "Archipelago.Instance.Session != null");
        
        // Sync items host -> client
        var hostItems = new Dictionary<string, int>();
        while (APItems.PeekItem() is { } itemInfo)
        {
            var itemName = itemInfo.ItemName;
            if (!hostItems.TryAdd(itemName, 1))
                hostItems[itemName]++;
            if (!AppliedInventory.TryGetValue(itemName, out var count) || hostItems[itemName] > count)
            {
                var sender = itemInfo.Player.Name;
                ModEntry.Instance.Logger.LogInformation("Sync: Received {item} from {player}", itemName, sender);
                ItemApplier.ApplyReceivedItem((itemName, sender));
            }
            APItems.DequeueItem();
        }
        ModEntry.Instance.Logger.LogDebug("Host Items: {hostItems}", hostItems);
        
        // We don't sync items client -> host, because it shouldn't be possible to receive items while disconnected
        
        // Sync locations client -> host
        var hostLocationsChecked = Archipelago.Instance.Session.Locations.AllLocationsChecked;
        var locationsToSync = LocationsChecked
            .Select(name => Archipelago.Instance.Session.Locations.GetLocationIdFromName("Cobalt Core", name))
            .Except(hostLocationsChecked);
        ModEntry.Instance.Logger.LogDebug("Client Locations to Send: {locationsToSync}", locationsToSync);
        Archipelago.Instance.CheckLocationsForced(locationsToSync.ToArray());
        
        // Sync locations host -> client (in case of when using a new save for example)
        var locationsToReceive = hostLocationsChecked
            .Select(id => Archipelago.Instance.Session.Locations.GetLocationNameFromId(id, "Cobalt Core"));
        ModEntry.Instance.Logger.LogDebug("Host Locations before Sync: {hostLocationsChecked}", hostLocationsChecked);
        LocationsChecked.UnionWith(locationsToReceive);
        
        Save();
    }

    internal void AddAppliedItem(string name)
    {
        if (!AppliedInventory.TryAdd(name, 1))
            AppliedInventory[name]++;
        Save();
    }

    internal bool AddCheckedLocations(params string[] name)
    {
        var added = name.Except(LocationsChecked).Any();
        LocationsChecked.UnionWith(name);
        if (added)
        {
            Save();
            return true;
        }
        return false;
    }

    internal bool HasItem(string name) => AppliedInventory.TryGetValue(name, out var value) && value > 0;
    internal bool HasCard(Type type) => Archipelago.CardToItem.TryGetValue(type, out var value)
                                        && HasItem(value);
    internal bool HasCardOrNotAP(Type type) => !Archipelago.CardToItem.TryGetValue(type, out var value)
                                               || HasItem(value);
    internal bool HasArtifact(Type type) => Archipelago.ArtifactToItem.TryGetValue(type, out var value)
                                            && HasItem(value);
    internal bool HasArtifactOrNotAP(Type type) => !Archipelago.ArtifactToItem.TryGetValue(type, out var value)
                                                   || HasItem(value);
    internal bool HasModifier(Type type) => Archipelago.ModifierToItem.TryGetValue(type, out var value)
                                            && HasItem(value);
    internal bool HasChar(Deck deck) => HasItem(Archipelago.ItemToDeck.FirstOrNull(kvp => kvp.Value == deck)?.Key ?? "");
    internal bool HasShip(string shipkey) => HasItem(Archipelago.ItemToStartingShip.FirstOrNull(kvp => kvp.Value == shipkey)?.Key ?? "");

    internal int GetFixTimelineAmountIfShuffled(Deck deck)
    {
        if (!Archipelago.InstanceSlotData.ShuffleMemories)
            ModEntry.Instance.Logger.LogWarning("Called GetFixTimelineCount, but ShuffleMemories is false. Possible bug?");
        var deckName = Archipelago.DeckToItem[deck];
        // Look among locations checked to see which is the last
        for (var i = 1; i <= 3; i++)
        {
            var locationName = $"Fix {deckName}'s Timeline {i}";
            if (!LocationsChecked.Contains(locationName))
                return i - 1;
        }
        return 3;
    }

    internal int GetFixTimelineAmountIfNotShuffled(Deck deck, State state)
    {
        return state.persistentStoryVars.memoryUnlockLevel.GetValueOrDefault(deck, 0);
    }

    internal (string location, int memoryIdx)? GetNextFixTimelineLocationNameIfShuffled(Deck deck)
    {
        var deckName = Archipelago.DeckToItem[deck];
        var currAmount = GetFixTimelineAmountIfShuffled(deck);
        if (currAmount >= 3) return null;
        var nextAmount = currAmount + 1;
        return ($"Fix {deckName}'s Timeline {nextAmount}", nextAmount);
    }
}

internal enum CardScoutMode
{
    DontScout = 0,
    ScoutOnly,
    CreateHint
}

internal enum DeathLinkMode
{
    Off = 0,
    Missing,
    HullDamage,
    HullDamagePercent,
    Death
}
