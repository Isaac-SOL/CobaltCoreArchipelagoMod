using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;

namespace CobaltCoreArchipelago.MenuPatches;

public class ProfileSelectPatches;

[HarmonyPatch(typeof(ProfileSelect), nameof(ProfileSelect.MkSlot))]
public class MkSlotPatch
{
    internal static Spr ArchipelagoSaveSpr, NotArchipelagoSaveSpr;
    
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> storedInstructions = new(instructions);
        var codeMatcher = new CodeMatcher(storedInstructions, generator);
        // Add a function call at the end, so that we can capture local variables
        codeMatcher.MatchStartForward(
                CodeMatch.WithOpcodes([OpCodes.Endfinally]) // Doesn't work with Ret for some reason
            ).ThrowIfInvalid("Could not find return instruction")
            .InsertAndAdvance(
                CodeInstruction.LoadArgument(3),
                CodeInstruction.LoadArgument(5),
                CodeInstruction.LoadLocal(3),
                CodeInstruction.Call((State.SaveSlot st, int n, SharedArt.ButtonResult slotButton)
                                         => PostMakeButton(st, n, slotButton))
            );
        return codeMatcher.Instructions();
    }
    
    static void PostMakeButton(State.SaveSlot st, int n, SharedArt.ButtonResult slotButton)
    {
        if (st.state is null) return;
        var shownSpr = APSaveData.AllAPSaves.ContainsKey(n) ? ArchipelagoSaveSpr : NotArchipelagoSaveSpr;
        Draw.Sprite(shownSpr, slotButton.v.x + 180.0, slotButton.v.y + (slotButton.isHover ? 3.0 : 2.0));
    }
}