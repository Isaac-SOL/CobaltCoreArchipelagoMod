using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using CobaltCoreArchipelago.MenuPatches;
using FMOD;
using HarmonyLib;
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
    static bool Prefix() => false;  // TODO Books was unlocked by herself for some reason ???
}

[HarmonyPatch(typeof(StoryVars), nameof(StoryVars.UnlockShip))]
public class UnlockShipPatch
{
    static bool Prefix() => false;
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
            UnlockReplacements.UnlockOneMemory(__instance, deck);
        }
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
        s.storyVars.unlockedChars.Add(deck);
    }
    
    internal static void UnlockShip(State s, string shipkey)
    {
        s.storyVars.unlockedShips.Add(shipkey);
    }
    
    internal static void UnlockOneMemory(StoryVars storyVars, Deck deck)
    {
        storyVars.memoryUnlockLevel.TryAdd(deck, 0);
        storyVars.memoryUnlockLevel[deck]++;
        
        // Immediately check for goal if we don't have to do the future memory
        // We can't have state in the arguments because of UnlockOneMemoryPatch, so we create a dummy state here
        var dummyState = new State { persistentStoryVars = storyVars };
        if (Archipelago.InstanceSlotData.DoFutureMemory || !VaultRenderPatch.CanCompleteGame(Vault.GetVaultMemories(dummyState)))
            return;
        Debug.Assert(Archipelago.Instance.Session != null, "Archipelago.Instance.Session != null");
        Archipelago.Instance.Session.SetGoalAchieved();
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
