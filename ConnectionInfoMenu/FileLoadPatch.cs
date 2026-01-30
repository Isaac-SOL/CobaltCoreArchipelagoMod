using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;

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
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> storedInstructions = new(instructions);
        var codeMatcher = new CodeMatcher(storedInstructions, generator);
        // Before storing the state, we pass it to a function to set the Archipelago values
        codeMatcher.MatchStartForward(
                CodeMatch.WithOpcodes([OpCodes.Stloc_0])
            ).ThrowIfInvalid("Could not find state store instructions")
            .RemoveInstructions(20) // Remove call to PopulateRun and skipTutorial set
            .InsertAndAdvance(
                CodeInstruction.Call<State, State>(state => EditStateForNewFile(state)),
                CodeInstruction.StoreLocal(0)
            );

        return codeMatcher.Instructions();
    }

    private static readonly string[] dontMarkSeenKeys =
    [
        // Actual character memory cutscenes
        "Dizzy_Memory", "Riggs_Memory", "Peri_Memory", "Goat_Memory",
        "Eunice_Memory", "Hacker_Memory", "Shard_Memory", "CAT_Memory",
        // Dialogue branches to pick a character at the end of a run
        "RunWinWho"
    ];

    public static State EditStateForNewFile(State state)
    {
        state.storyVars.skipTutorial = true;
        var startingChars = Archipelago.InstanceSlotData.StartingCharacters;
        state.storyVars.unlockedChars = new HashSet<Deck>(startingChars);
        state.storyVars.unlockedShips = [Archipelago.InstanceSlotData.StartingShip];
        state.runConfig.selectedChars = new HashSet<Deck>(startingChars);
        state.runConfig.selectedShip = Archipelago.InstanceSlotData.StartingShip;
        // Based on Cheat.UnlockAllContent
        state.storyVars.winCount = 500;  // Forces vault button to be visible.
        foreach (var node in DB.story.all.Where(kvp => !dontMarkSeenKeys.Any(s => kvp.Key.StartsWith(s))))
            DB.story.MarkNodeSeen(state, node.Key);  // We view all story nodes but exclude some
        foreach (var kvp in DB.enemies)
            state.storyVars.RecordEnemyDefeated(kvp.Key);  // No idea but just in case
        // Start a loop and end it immediately
        state.PopulateRun(StarterShip.ships["artemis"], chars: startingChars);
        state.ship.hull = 0;
        state.storyVars.ResetAfterRun();
        state.ChangeRoute(() => new NewRunOptions());
        return state;
    }
    
}

[HarmonyPatch(typeof(State), nameof(State.PopulateRun))]
public class PopulateRunPatch
{
    static void Prefix(State __instance, ref int difficulty)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");

        __instance.storyVars.spikeName = new List<StoryVars.SpikeNames>
        {
            StoryVars.SpikeNames.spiketwo,
            StoryVars.SpikeNames.george
        }.Random(__instance.rngShuffle);
    }
    
}