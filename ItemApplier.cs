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
            UnlockReplacements.UnlockShip(state, ship);
        }
        else if (Archipelago.ItemToDeck.TryGetValue(name, out var deck))
        {
            UnlockReplacements.UnlockChar(state, deck);
        }
        else if (Archipelago.ItemToMemory.TryGetValue(name, out var deckMemory))
        {
            UnlockReplacements.UnlockOneMemory(state, deckMemory);
        }
        else if (Archipelago.ItemToCard.TryGetValue(name, out var card))
        {
            // Also unlock cards in current deck if applicable
            UnlockReplacements.UnlockCodexCard(state, card);
        }
        else if (Archipelago.ItemToArtifact.TryGetValue(name, out var artifact))
        {
            // Also unlock artifacts in current deck if applicable
            UnlockReplacements.UnlockCodexArtifact(state, artifact);
        }

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