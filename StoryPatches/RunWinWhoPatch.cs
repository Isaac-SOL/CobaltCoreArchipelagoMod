using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nickel;

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
                // Get a lambda defined in GetChoices returning a Choice
                .First(m => m.Name.StartsWith($"<{nameof(RunWinHelpers.GetChoices)}>") && m.ReturnType == typeof(Choice)),
            transpiler: new HarmonyMethod(typeof(RunWinWhoPatch).GetMethod(nameof(Transpiler)))
        );
    }
    
    // Allow CAT and Books to appear on the final choices thrice like other characters
    // (But only if we have AddCharacterMemories)
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> storedInstructions = new(instructions);
        var codeMatcher = new CodeMatcher(storedInstructions, generator);
        // Remove switch on CAT and Books
        codeMatcher.MatchStartForward(
                CodeMatch.WithOpcodes([OpCodes.Ldloc_0]),
                CodeMatch.WithOpcodes([OpCodes.Brtrue_S])
            ).ThrowIfInvalid("Could not find switch in instructions")
            .RemoveInstructions(17)
            // Replace with our own check instead
            // NOTE: The cursor is currently on the first instruction of the next section.
            // Adding a label applies to that instruction, but inserting adds instructions BEFORE it.
            .CreateLabel(out var nextLabel) // Create label to jump to next section
            .InsertAndAdvance(
                CodeInstruction.LoadLocal(0),
                CodeInstruction.Call<Deck, bool>(deck => SkipCharChoice(deck)),
                new CodeInstruction(OpCodes.Brfalse_S, nextLabel),  // If we don't skip, jump to next section
                new CodeInstruction(OpCodes.Ldnull),
                new CodeInstruction(OpCodes.Ret)                    // If we do skip, return null
            );
        return codeMatcher.Instructions();
    }

    // We skip if it's Books or CAT and we haven't added memories for them
    public static bool SkipCharChoice(Deck deck)
    {
        return !Archipelago.InstanceSlotData.AddCharacterMemories && deck is Deck.colorless or Deck.shard;
    }
}