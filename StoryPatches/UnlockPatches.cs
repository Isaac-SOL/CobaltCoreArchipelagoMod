using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

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