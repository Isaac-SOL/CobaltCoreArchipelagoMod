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
        foreach (var unlockedChar in __instance.unlockedChars)
        {
            __result.Add(unlockedChar);
        }
    }
}

[HarmonyPatch(typeof(StoryVars), nameof(StoryVars.GetUnlockedShips))]
public class GetUnlockedShipsPatch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> storedInstructions = new(instructions);
        var codeMatcher = new CodeMatcher(storedInstructions, generator);
        // Remove HashSet fill with artemis
        codeMatcher.MatchStartForward(
                CodeMatch.WithOpcodes([OpCodes.Newobj])
            ).ThrowIfInvalid("Could not find HashSet creation in instructions")
            .Advance()
            .RemoveInstructions(4);
        return codeMatcher.Instructions();
    }
}

// Kill all unlocks directly at the method call, should be easier

[HarmonyPatch(typeof(StoryVars), nameof(StoryVars.UnlockChar))]
public class UnlockCharPatch
{
    // Nickel seems to be interfering with this function, so we let it happen and then clear if necessary in postfix
    [HarmonyPriority(Priority.Low)]
    static void Postfix(ref StoryVars __instance, Deck deck)
    {
        if (Archipelago.Instance.APSaveData is null) return;  // May be called before a slot is loaded ?
        // Rewrite unlockedChars entirely from AP inventory
        RewriteUnlockedCharsFromAP(__instance);
        ModEntry.Instance.Logger.LogWarning("Called UnlockCharPatch on {deck}", deck);
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
    static void Postfix(ref StoryVars __instance, string shipkey)
    {
        if (Archipelago.Instance.APSaveData is null) return;  // May be called before a slot is loaded ?
        // Rewrite unlockedShips entirely from AP inventory
        RewriteUnlockedShipsFromAP(__instance);
        ModEntry.Instance.Logger.LogWarning("Called UnlockShipPatch on {ship}", shipkey);
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
        }
        else
        {
            // If memories aren't shuffled, we pass through to our replacement function
            // and use the saved data directly from Cobalt Core
            var s = MG.inst.g.state; // This is our only way to access state here
            var count = __instance.memoryUnlockLevel.TryGetValue(deck, out var currCount) ? currCount + 1 : 1;
            UnlockReplacements.SetMemoryCount(s, deck, count);
        }
        ModEntry.Instance.Logger.LogWarning("Called UnlockOneMemoryPatch on {memory}", deck);
        return false;
    }
}

[HarmonyPatch(typeof(State), nameof(State.OnHasCard))]
public class OnHasCardPatch
{
    static bool Prefix(Card card)
    {
        // Allow unlocking cards that aren't archipelago items
        return !Archipelago.InstanceSlotData.ShuffleCards || !Archipelago.CardToItem.ContainsKey(card.GetType());
    }
}

[HarmonyPatch(typeof(State), nameof(State.OnHasArtifact))]
public class OnHasArtifactPatch
{
    static bool Prefix(Artifact r)
    {
        // Allow unlocking artifacts that aren't archipelago items
        return !Archipelago.InstanceSlotData.ShuffleArtifacts || !Archipelago.ArtifactToItem.ContainsKey(r.GetType());
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
