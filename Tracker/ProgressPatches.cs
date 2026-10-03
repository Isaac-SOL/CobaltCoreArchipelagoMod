using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Helpers;
using daisyowl.text;
using HarmonyLib;

namespace CobaltCoreArchipelago;

[HarmonyPatch(typeof(Progress), nameof(Progress.Render))]
public class ProgressRenderPatch
{
    private static ILocationCheckHelper Locations => Archipelago.Instance.Session!.Locations;
    
    static bool Prefix(G g, State state)
    {
        var allLocations = Locations.AllLocations
            .Select(l => Locations.GetLocationNameFromId(l))
            .ToHashSet();
        var allCheckedLocations = Locations.AllLocationsChecked
            .Select(l => Locations.GetLocationNameFromId(l))
            .ToHashSet();
        Progress.ProgressItem(g, 0, Loc.T("progress.cards", "Cards Found"),
                              allCheckedLocations.Count(s => s.Contains("Card")),
                              allLocations.Count(s => s.Contains("Card")));
        Progress.ProgressItem(g, 1, Loc.T("progress.artifacts", "Artifacts Found"),
                              allCheckedLocations.Count(s => s.Contains("Artifact")),
                              allLocations.Count(s => s.Contains("Artifact")));
        if (Archipelago.InstanceSlotData.ShuffleMemories)
        {
            Progress.ProgressItem(g, 2, Loc.T("progress.memories", "Memories Unlocked"),
                                  allCheckedLocations.Count(s => s.StartsWith("Fix ")),
                                  allLocations.Count(s => s.StartsWith("Fix ")));
        }
        else
        {
            var vaultMemories = Vault.GetVaultMemories(state);
            Progress.ProgressItem(g, 2, Loc.T("progress.memories", "Memories Unlocked"),
                                  vaultMemories.Sum(ms => ms.memoryKeys.Count(m => m.unlocked)),
                                  vaultMemories.Sum(ms => ms.memoryKeys.Count));
        }
        
        Draw.Text("See a more detailed summary\nin the <c=boldPink>Archipelago Tracker</c> in the Codex.",
                  240.0, 175.0,
                  align: TAlign.Center, color: Colors.buttonBoxNormal, outline: Colors.black);
        return false;
    }
}