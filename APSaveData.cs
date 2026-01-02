using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Nickel;

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
    [JsonIgnore] private static Dictionary<int, APSaveData>? _allApSaveStorage;
    
    [JsonProperty]
    internal int SaveSlot { get; }
    [JsonProperty]
    internal string Hostname { get; }
    [JsonProperty]
    internal int Port { get; }
    [JsonProperty]
    internal string Slot { get; }
    [JsonProperty]
    internal string? Password { get; }
    [JsonProperty]
    internal string? RoomId { get; set; }
    [JsonProperty]
    internal Dictionary<string, int> AppliedInventory { get; }
    [JsonProperty]
    internal HashSet<string> LocationsChecked { get; }
    
    internal static IModStorage ModStorage => ModEntry.Instance.Helper.Storage;

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
            value = new APSaveData(saveSlot, "localhost", 38281, "Time Crystal");
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
}