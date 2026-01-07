using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace CobaltCoreArchipelago.ConnectionInfoMenu;

[HarmonyPatch(typeof(ProfileSelect), nameof(ProfileSelect.SelectSlot))]
public class SelectSlotPatch
{
    static bool Prefix(ref G g, int slotIdx)
    {
        var saveSlot = State.Load(slotIdx);
        if (saveSlot.state is null)
            APSaveData.Erase(slotIdx);
        ModEntry.Instance.Archipelago.LoadSaveData(slotIdx);
        g.metaRoute!.subRoute = new ConnectionInfoInput
        {
            SlotIdx = slotIdx,
            SaveSlot = saveSlot
        };
        return false;
    }
    
    public static void SelectSlotReplacement(G g, int slotIdx)
    {
        g.settings.saveSlot = slotIdx;
        g.settings.Save();
        PFX.ClearAll();
        g.state = State.LoadOrNew(g.settings.saveSlot);
        Cheevos.CheckOnLoad(g.state);
        g.state.SaveIfRelease();
        g.metaRoute = new MainMenu();
    }
}

[HarmonyPatch(typeof(G), nameof(G.LoadSavegameOnStartup))]
public class StartupFileLoadPatch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,  ILGenerator generator)
    {
        List<CodeInstruction> storedInstructions = new(instructions);
        var codeMatcher = new CodeMatcher(storedInstructions, generator);
        // Remove the branch, so that the game always starts on the profile select screen
        codeMatcher.MatchStartForward(
                CodeMatch.LoadsLocal(),
                CodeMatch.WithOpcodes([OpCodes.Ldfld]),
                CodeMatch.Branches()
            ).ThrowIfInvalid("Could not find branch in instructions")
            .RemoveInstructions(3);
        return codeMatcher.Instructions();
    }
}

[HarmonyPatch(typeof(State), nameof(State.NewGame))]
public class NewGamePatch
{
    // ReSharper disable once RedundantAssignment
    static void Prefix(ref bool skipTutorial)
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
        return Archipelago.InstanceSlotData.StartingShip;
    }

    public static int GetArchipelagoStartingCharacter(int pos)
    {
        return (int)Archipelago.InstanceSlotData.StartingCharacters[pos];
    }
    
}

[HarmonyPatch(typeof(State), nameof(State.PopulateRun))]
public class PopulateRunPatch
{
    static void Prefix(State __instance, ref IEnumerable<Deck>? chars, ref bool giveRunStartRewards, ref int difficulty)
    {
        // giveRunStartRewards = true;
        if (chars == null)
        {
            // Should only happen in a new game
            chars = Archipelago.InstanceSlotData.StartingCharacters;
            __instance.storyVars.unlockedChars = new HashSet<Deck>(chars);
            __instance.storyVars.unlockedShips = [Archipelago.InstanceSlotData.StartingShip];
        }

        if (difficulty < Archipelago.InstanceSlotData.MinimumDifficulty)
        {
            difficulty = Archipelago.InstanceSlotData.MinimumDifficulty;
        }
    }
    
}