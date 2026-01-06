using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using FMOD;
using HarmonyLib;
using Debug = System.Diagnostics.Debug;

namespace CobaltCoreArchipelago.StoryPatches;

public class UnlockPatches;

[HarmonyPatch(typeof(StoryVars), nameof(StoryVars.GetUnlockedChars))]
public class GetUnlockedCharsPatch
{
    
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        // TODO Doesn't work - forced active in a different patch? (on character select screen)
        List<CodeInstruction> storedInstructions = new(instructions);
        var codeMatcher = new CodeMatcher(storedInstructions, generator);
        // Remove HashSet fill with Dizzy, Riggs, Peri
        codeMatcher.MatchStartForward(
                CodeMatch.WithOpcodes([OpCodes.Newobj])
            ).ThrowIfInvalid("Could not find HashSet creation in instructions")
            .Advance()
            .RemoveInstructions(12);
        return codeMatcher.Instructions();
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
    static bool Prefix() => false;
}

[HarmonyPatch(typeof(StoryVars), nameof(StoryVars.UnlockShip))]
public class UnlockShipPatch
{
    static bool Prefix() => false;
}

[HarmonyPatch(typeof(StoryVars), nameof(StoryVars.UnlockOneMemory))]
public class UnlockOneMemoryPatch
{
    static bool Prefix() => false;
}

[HarmonyPatch(typeof(State), nameof(State.OnHasCard))]
public class OnHasCardPatch
{
    static bool Prefix(Card card)
    {
        // Allow unlocking cards that aren't archipelago items
        return !Archipelago.CardToItem.ContainsKey(card.GetType());
    }
}

[HarmonyPatch(typeof(State), nameof(State.OnHasArtifact))]
public class OnHasArtifactPatch
{
    static bool Prefix(Artifact r)
    {
        // Allow unlocking artifacts that aren't archipelago items
        return !Archipelago.ArtifactToItem.ContainsKey(r.GetType());
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
    
    internal static void UnlockOneMemory(State s, Deck deck)
    {
        var storyVars = s.storyVars;
        storyVars.memoryUnlockLevel.TryAdd(deck, 0);
        storyVars.memoryUnlockLevel[deck]++;
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
