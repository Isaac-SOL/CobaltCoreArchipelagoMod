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

        harmony.Patch(
            original: typeof(RunWinHelpers).GetMethod(nameof(RunWinHelpers.GetChoices)),
            postfix: new HarmonyMethod(typeof(RunWinWhoPatch).GetMethod(nameof(GetChoicesPostfix)))
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

    public static void GetChoicesPostfix(List<Choice> __result, State s)
    {
        // Show current memory counts for each character
        var decksToAdd = new List<Deck>();
        foreach (var choice in __result)
        {
            var deck = choice.actions.Count > 0 ? (choice.actions[0] as ARunWinCharChoice)?.deck : null;
            if (deck is null) continue;
            choice.label += $" ({s.persistentStoryVars.memoryUnlockLevel.GetValueOrDefault(deck.Value, 0)}/3)";
            decksToAdd.Add(deck.Value);
        }
        
        // Add the choice to get all memories
        if (!Archipelago.InstanceSlotData.UnlockMemoryForAllCharacters || decksToAdd.Count <= 1) return;
        
        __result.Add(new Choice
        {
            label = ModEntry.Instance.Localizations.Localize(["story", "memory", decksToAdd.Count == 2 ? "twoChoice" : "allChoice"]),
            key = ".runWin_AllOfThem",
            actions = decksToAdd
                .Select(deck => new ARunWinCharChoice
                {
                    deck = deck
                })
                .Cast<CardAction>()
                .ToList()
        });
    }
}
