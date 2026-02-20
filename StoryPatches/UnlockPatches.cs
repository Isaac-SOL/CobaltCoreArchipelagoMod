using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using CobaltCoreArchipelago.MenuPatches;
using FMOD;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Debug = System.Diagnostics.Debug;

namespace CobaltCoreArchipelago.StoryPatches;

public class UnlockPatches;

[HarmonyPatch(typeof(StoryVars), nameof(StoryVars.GetUnlockedChars))]
public class GetUnlockedCharsPatch
{
    // Remove Dizzy, Riggs and Peri from the default unlocked characters.
    // Nickel does stuff to this function, so we let it run, then we clear the result and replace it with our own
    [HarmonyPriority(Priority.Low)]
    static void Postfix(ref HashSet<Deck> __result, StoryVars __instance)
    {
        __result.Clear();
        UnlockCharPatch.RewriteUnlockedCharsFromAP(__instance);
        foreach (var unlockedChar in __instance.unlockedChars)
        {
            __result.Add(unlockedChar);
        }
    }
}

[HarmonyPatch(typeof(StoryVars), nameof(StoryVars.GetUnlockedShips))]
public class GetUnlockedShipsPatch
{
    [HarmonyPriority(Priority.Low)]
    static void Postfix(ref HashSet<string> __result, StoryVars __instance)
    {
        __result.Clear();
        UnlockShipPatch.RewriteUnlockedShipsFromAP(__instance);
        foreach (var unlockedShip in __instance.unlockedShips)
        {
            __result.Add(unlockedShip);
        }
    }
}

// Kill all unlocks directly at the method call, should be easier

[HarmonyPatch(typeof(StoryVars), nameof(StoryVars.UnlockChar))]
public class UnlockCharPatch
{
    // Nickel seems to be interfering with this function, so we let it happen and then clear if necessary in postfix
    [HarmonyPriority(Priority.Low)]
    static void Postfix(ref StoryVars __instance)
    {
        if (Archipelago.Instance.APSaveData is null) return;  // May be called before a slot is loaded ?
        // Rewrite unlockedChars entirely from AP inventory
        RewriteUnlockedCharsFromAP(__instance);
    }

    internal static void RewriteUnlockedCharsFromAP(StoryVars storyVars)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        storyVars.unlockedChars = Archipelago.Instance.APSaveData.FoundChars.ToHashSet();
        storyVars.unlockedCharsToAnnounce = storyVars.unlockedCharsToAnnounce
            .Where(deck => Archipelago.Instance.APSaveData.HasChar(deck))
            .ToList();
    }
}

[HarmonyPatch(typeof(StoryVars), nameof(StoryVars.UnlockShip))]
public class UnlockShipPatch
{
    // Nickel seems to be interfering with this function, so we let it happen and then clear if necessary in postfix
    [HarmonyPriority(Priority.Low)]
    static void Postfix(ref StoryVars __instance)
    {
        if (Archipelago.Instance.APSaveData is null) return;  // May be called before a slot is loaded ?
        // Rewrite unlockedShips entirely from AP inventory
        RewriteUnlockedShipsFromAP(__instance);
    }

    internal static void RewriteUnlockedShipsFromAP(StoryVars storyVars)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        storyVars.unlockedShips = Archipelago.Instance.APSaveData.FoundShips.ToHashSet();
        storyVars.unlockedShipsToAnnounce = storyVars.unlockedShipsToAnnounce
            .Where(shipkey => Archipelago.Instance.APSaveData.HasShip(shipkey))
            .ToList();
    }
}

// Recheck right before we have to show the unlocked chars/ships

[HarmonyPatch(typeof(UnlockedDeck), nameof(UnlockedDeck.MakeIfHasRewards))]
public class PreCheckUnlockedDecksPatch
{
    [HarmonyPriority(Priority.High)]
    static void Prefix(State s)
    {
        UnlockCharPatch.RewriteUnlockedCharsFromAP(s.storyVars);
    }
}

[HarmonyPatch(typeof(UnlockedShip), nameof(UnlockedShip.MakeIfHasRewards))]
public class PreCheckUnlockedShipsPatch
{
    [HarmonyPriority(Priority.High)]
    static void Prefix(State s)
    {
        UnlockShipPatch.RewriteUnlockedShipsFromAP(s.storyVars);
    }
}

[HarmonyPatch(typeof(StoryVars), nameof(StoryVars.UnlockOneMemory))]
public class UnlockOneMemoryPatch
{
    static bool Prefix(StoryVars __instance, Deck deck)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        if (Archipelago.InstanceSlotData.ShuffleMemories)
        {
            // If memories are shuffled, we send the corresponding location
            var location = Archipelago.Instance.APSaveData.GetNextFixTimelineLocationName(deck);
            if (location is not null)
                Archipelago.Instance.CheckLocation(location);
            else
                ModEntry.Instance.Logger.LogError("Could not find location to check on memory unlock with deck {deck}", deck);
        }
        else
        {
            // If memories aren't shuffled, we pass through to our replacement function
            // and use the saved data directly from Cobalt Core
            var s = MG.inst.g.state; // This is our only way to access state here
            var count = __instance.memoryUnlockLevel.TryGetValue(deck, out var currCount) ? currCount + 1 : 1;
            UnlockReplacements.SetMemoryCount(s, deck, count);
        }
        return false;
    }
}

// Then we rewrite our own versions so we can actually unlock stuff ourselves
internal static class UnlockReplacements
{
    internal static void UnlockChar(State s, Deck deck)
    {
        if (s.storyVars.unlockedChars.Add(deck))
            s.storyVars.unlockedCharsToAnnounce.Add(deck);
    }
    
    internal static void UnlockShip(State s, string shipkey)
    {
        if (s.storyVars.unlockedShips.Add(shipkey))
            s.storyVars.unlockedShipsToAnnounce.Add(shipkey);
    }
    
    // Called either by the normal UnlockOneMemory if memories aren't shuffled, otherwise by ApplyItems if they are
    internal static void SetMemoryCount(State s, Deck deck, int count)
    {
        s.storyVars.memoryUnlockLevel[deck] = count;
        
        // Immediately check for goal if we don't have to do the future memory
        if (!Archipelago.InstanceSlotData.DoFutureMemory &&
            VaultRenderPatch.CanCompleteGame(Vault.GetVaultMemories(s)))
        {
            Debug.Assert(Archipelago.Instance.Session != null, "Archipelago.Instance.Session != null");
            Archipelago.Instance.Session.SetGoalAchieved();
        }
    }

    internal static void UnlockCodexCard(State s, Type cardType)
    {
        var card = cardType.CreateInstance() as Card;
        Debug.Assert(card != null, nameof(card) + " != null");
        s.storyVars.cardsOwned.Add(card.Key());
    }

    internal static void UnlockCodexArtifact(State s, Type artifactType)
    {
        var artifact = artifactType.CreateInstance() as Artifact;
        Debug.Assert(artifact != null, nameof(artifact) + " != null");
        s.storyVars.artifactsOwned.Add(artifact.Key());
    }
}
