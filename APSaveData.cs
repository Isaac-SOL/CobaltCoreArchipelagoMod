using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    internal bool DeathLinkActive { get; set; } = true;
    [JsonProperty]
    internal CardScoutMode CardScoutMode { get; set; } = CardScoutMode.CreateHint;
    [JsonProperty]
    internal bool BypassDifficulty { get; set; } = false;
    
    internal static IModStorage ModStorage => ModEntry.Instance.Helper.Storage;

    [JsonIgnore]
    internal IEnumerable<Type> FoundCards => AppliedInventory
        .Where(kvp => kvp.Value > 0)
        .Select(kvp => kvp.Key)
        .Intersect(Archipelago.ItemToCard.Keys)
        .Select(s => Archipelago.ItemToCard[s]);
    [JsonIgnore]
    internal IEnumerable<Type> FoundArtifacts => AppliedInventory
        .Where(kvp => kvp.Value > 0)
        .Select(kvp => kvp.Key)
        .Intersect(Archipelago.ItemToArtifact.Keys)
        .Select(s => Archipelago.ItemToArtifact[s]);

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
    }

    internal static void LoadAllSaves()
    {
        ModEntry.Instance.Logger.LogInformation(ModStorage.GetMainStorageFile("json").FullName);
        if (!ModStorage.TryLoadJson(ModStorage.GetMainStorageFile("json"), out _allApSaveStorage) || _allApSaveStorage is null)
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

    internal void SyncWithHost()
    {
        Debug.Assert(Archipelago.Instance.Session != null, "Archipelago.Instance.Session != null");
        var localItemCount = AppliedInventory.Sum(kvp => kvp.Value);
        var hostItemCount = Archipelago.Instance.Session.Items.AllItemsReceived.Count;

        // Apparently this case might be fine so don't check for it
        // Debug.Assert(localItemCount <= hostItemCount, "localItemCount <= hostItemCount");
        if (localItemCount < hostItemCount)
        {
            foreach (var itemInfo in Archipelago.Instance.Session.Items.AllItemsReceived)
            {
                SyncItemCountWithHost(itemInfo.ItemName);
            }
        }

        var hostLocationsChecked = Archipelago.Instance.Session.Locations.AllLocationsChecked;
        var locationsToSync = LocationsChecked.Select(name => Archipelago.Instance.Session.Locations
                                                          .GetLocationIdFromName("Cobalt Core", name))
            .Where(address => !hostLocationsChecked.Contains(address));
        Archipelago.Instance.CheckLocationsForced(locationsToSync.ToArray());
        
        Save();
    }

    internal void SyncItemCountWithHost(string name)
    {
        Debug.Assert(Archipelago.Instance.Session != null, "Archipelago.Instance.Session != null");
        var localCount = AppliedInventory.GetValueOrDefault(name, 0);
        var hostCount = Archipelago.Instance.Session.Items.AllItemsReceived.Count(i => i.ItemName == name);
        for (int i = localCount; i < hostCount; i++)
        {
            ItemApplier.ApplyReceivedItem(name);
        }
    }

    internal void AddAppliedItem(string name)
    {
        if (AppliedInventory.ContainsKey(name))
            AppliedInventory[name]++;
        else
            AppliedInventory[name] = 1;
        Save();
    }

    internal bool AddCheckedLocation(string name)
    {
        if (LocationsChecked.Add(name))
        {
            Save();
            return true;
        }
        return false;
    }

    internal bool HasItem(string name) => AppliedInventory.TryGetValue(name, out var value) && value > 0;
    internal bool HasCard(Type type) => Archipelago.CardToItem.TryGetValue(type, out var value) && HasItem(value);
    internal bool HasArtifact(Type type) => Archipelago.ArtifactToItem.TryGetValue(type, out var value) && HasItem(value);

    internal string? GetNextFixTimelineLocationName(Deck deck)
    {
        // Look among locations checked to see which is the next location
        var name = Archipelago.ItemToDeck.First(kvp => kvp.Value == deck).Key;
        var baseLocationName = $"Fix {name}'s Timeline";
        for (var i = 1; i <= 3; i++)
        {
            var locationName = $"{baseLocationName} {i}";
            if (!LocationsChecked.Contains(locationName))
                return locationName;
        }
        return null;
    }
}

internal enum CardScoutMode
{
    DontScout = 0,
    ScoutOnly,
    CreateHint
}
