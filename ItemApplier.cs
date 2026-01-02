using System.Collections.Generic;
using System.Diagnostics;
using CobaltCoreArchipelago.StoryPatches;
using Microsoft.Extensions.Logging;

namespace CobaltCoreArchipelago;

public static class ItemApplier
{
    internal static List<string> DeferredUnappliedItems { get; } = [];
    
    internal static bool CanApplyItems() => true;
    
    internal static void ApplyReceivedItem(string name, State? state = null)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        if (state is null || !CanApplyItems())
        {
            DeferredUnappliedItems.Add(name);
            return;
        }
        
        if (Archipelago.ItemToStartingShip.TryGetValue(name, out var ship))
        {
            state.storyVars.UnlockShip(ship);
        }
        else if (Archipelago.ItemToDeck.TryGetValue(name, out var deck))
        {
            state.storyVars.UnlockChar(deck);
        }
        else if (Archipelago.ItemToCard.ContainsKey(name))
        {
            
        }
        else if (Archipelago.ItemToArtifact.ContainsKey(name))
        {
            
        }
        // Also do memories

        Archipelago.Instance.APSaveData.AddAppliedItem(name);
    }

    internal static void ApplyDeferredItems(State state)
    {
        if (!CanApplyItems())
            ModEntry.Instance.Logger.LogWarning("Trying to apply deferred items while unable to apply them");
        var toApply = DeferredUnappliedItems;
        DeferredUnappliedItems.Clear();
        foreach (var item in toApply)
        {
            ApplyReceivedItem(item, state);
        }
    }
}