using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;

namespace CobaltCoreArchipelago.StoryPatches;

public class RunWinWhoPatch
{
    // Here we are editing the lambda inside RunWinHelpers.GetChoices so it's a bit trickier
    public static void ApplyPatch(Harmony harmony)
    {
        harmony.Patch(
            original: typeof(RunWinHelpers)
                .GetNestedTypes(AccessTools.all)
                .SelectMany(t => t.GetMethods(AccessTools.all))
                .First(m => m.Name.StartsWith("<GetChoices>") && m.ReturnType == typeof(Choice)),
            transpiler: new HarmonyMethod(typeof(RunWinWhoPatch).GetMethod("Transpiler"))
        );
    }
    
    // Allow CAT and Books to appear on the final choices thrice like other characters
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> storedInstructions = new(instructions);
        var codeMatcher = new CodeMatcher(storedInstructions, generator);
        // Remove switch on CAT and Books
        codeMatcher.MatchStartForward(
                CodeMatch.WithOpcodes([OpCodes.Ldloc_0]),
                CodeMatch.WithOpcodes([OpCodes.Brtrue_S])
            ).ThrowIfInvalid("Could not find switch in instructions")
            .RemoveInstructions(17);
        return codeMatcher.Instructions();
    }
}