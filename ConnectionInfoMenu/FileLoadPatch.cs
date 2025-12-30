using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace CobaltCoreArchipelago.ConnectionInfoMenu;

[HarmonyPatch(typeof(State), nameof(State.LoadOrNew))]
public class FileLoadPatch
{
    static void Prefix()
    {
        ModEntry.Instance.Archipelago.Reconnect("localhost", 38281, "Time Crystal");
    }
}

[HarmonyPatch(typeof(State), nameof(State.NewGame))]
public class NewGamePatch
{
    private static string test_field = "";
    
    // ReSharper disable once RedundantAssignment
    static void Prefix(int? slot, uint? seed, MapBase? map, ref bool skipTutorial)
    {
        skipTutorial = true;
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> storedInstructions = new(instructions);
        var codeMatcher = new CodeMatcher(storedInstructions, generator);
        // Load ship name from archipelago instead
        codeMatcher.MatchStartForward(
                CodeMatch.LoadsConstant("artemis")
            ).ThrowIfInvalid("Could not find artemis load call in instructions")
            .RemoveInstruction()
            .InsertAndAdvance(
                CodeInstruction.Call(string () => GetArchipelagoStartingShipName())
            );
        var searchedCodes = new List<OpCode>
        {
            OpCodes.Ldc_I4_1, OpCodes.Ldc_I4_2, OpCodes.Ldc_I4_3
        };
        // Load character IDs from archipelago instead
        for (int i = 0; i < searchedCodes.Count; i++)
        {
            var i1 = i;
            codeMatcher.MatchStartForward(
                    CodeMatch.WithOpcodes([OpCodes.Dup]),
                    CodeMatch.WithOpcodes([searchedCodes[i]])
                ).ThrowIfInvalid($"Could not find character {i} load call in instructions")
                .Advance()
                .RemoveInstruction()
                .InsertAndAdvance(
                    CodeInstruction.CallClosure(int () => GetArchipelagoStartingCharacter(i1)));
        }

        return codeMatcher.Instructions();
    }

    public static string GetArchipelagoStartingShipName()
    {
        return Archipelago.Instance.SlotDataHelper!.Value.StartingShip.ship.key;
    }

    public static int GetArchipelagoStartingCharacter(int pos)
    {
        return (int)Archipelago.Instance.SlotDataHelper!.Value.StartingCharacters[pos];
    }

    static void Postfix()
    {
        
    }
}