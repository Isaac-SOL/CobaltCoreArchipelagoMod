using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
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
        else if (!APSaveData.AllAPSaves.ContainsKey(slotIdx)) // Prevent loading non-archipelago saves
            return false;
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
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
        // Before storing the state, we pass it to a function to set the Archipelago values
        codeMatcher.MatchStartForward(
                CodeMatch.WithOpcodes([OpCodes.Stloc_0])
            ).ThrowIfInvalid("Could not find state store instructions")
            .InsertAndAdvance(
                CodeInstruction.Call<State, State>(state => EditStateForNewFile(state))
            );

        return codeMatcher.Instructions();
    }

    public static State EditStateForNewFile(State state)
    {
        state.runConfig.selectedShip = Archipelago.InstanceSlotData.StartingShip;
        state.runConfig.selectedChars = new HashSet<Deck>(Archipelago.InstanceSlotData.StartingCharacters);
        // Copied from Cheat.UnlockAllContent
        state.storyVars.winCount = 500;  // Forces vault button to be visible. 
        foreach (var kvp in DB.story.all)
            DB.story.MarkNodeSeen(state, kvp.Key);  // Mark all dialogue as seen TODO this causes mystery nodes to malfunction on first run
        foreach (var kvp in DB.enemies)
            state.storyVars.RecordEnemyDefeated(kvp.Key);  // No idea but just in case
        return state;
    }
    
}

[HarmonyPatch(typeof(State), nameof(State.PopulateRun))]
public class PopulateRunPatch
{
    static void Prefix(State __instance, ref IEnumerable<Deck>? chars, ref int difficulty)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        
        if (chars == null)
        {
            // Should only happen in a new game
            chars = Archipelago.InstanceSlotData.StartingCharacters;
            __instance.storyVars.unlockedChars = new HashSet<Deck>(chars);
            __instance.storyVars.unlockedShips = [Archipelago.InstanceSlotData.StartingShip];
        }

        if (!Archipelago.Instance.APSaveData.BypassDifficulty && difficulty < Archipelago.InstanceSlotData.MinimumDifficulty)
        {
            difficulty = Archipelago.InstanceSlotData.MinimumDifficulty;
        }
    }
    
}