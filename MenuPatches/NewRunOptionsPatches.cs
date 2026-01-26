using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using CobaltCoreArchipelago.StoryPatches;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;

namespace CobaltCoreArchipelago.MenuPatches;

public class NewRunOptionsPatches;

// No, dailies are never unlocked because we don't support them
[HarmonyPatch(typeof(NewRunOptions), nameof(NewRunOptions.AreDailiesUnlocked))]
public class DailiesPatch
{
    public static void Postfix(ref bool __result)
    {
        __result = false;
    }
}

// Actually just prevent dailies button from being drawn at all
[HarmonyPriority(Priority.High)]
[HarmonyPatch(typeof(SharedArt), nameof(SharedArt.ButtonText))]
public class ButtonTextPatch
{
    public static bool Prefix(UIKey key)
    {
        return key.k != StableUK.newRun_dailyRun;
    }
}

// Unhide Books and CAT
[HarmonyPatch(typeof(Character), nameof(Character.Render))]
public class CharacterRenderPatch
{
    public static void Prefix(ref bool hideFace)
    {
        hideFace = false;
    }
    
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> storedInstructions = new(instructions);
        var codeMatcher = new CodeMatcher(storedInstructions, generator);

        // Using Shrike here because I don't know how to use indices with CodeMatcher
        var seqMatched = new SequenceBlockMatcher<CodeInstruction>(storedInstructions)
            // Match sequence where the function adds the tooltip by checking showUnlockInstructions
            .Find(
                ILMatches.Ldloca(17),
                ILMatches.Call("GetValueOrDefault"),
                ILMatches.Stloc(16),
                ILMatches.Ldarg(12),
                ILMatches.Brfalse
            )
            .EncompassUntil(
                SequenceMatcherPastBoundsDirection.After,
                ILMatches.Ldarg(1),
                ILMatches.Ldfld(typeof(G).Field(nameof(G.tooltips))),
                ILMatches.Ldloc(14),
                ILMatches.Ldstr("char.desc.locked"),
                ILMatches.Call("AddGlossary")
            )
            .Remove(SequenceMatcherPastBoundsDirection.Before)
            .Insert(
                SequenceMatcherPastBoundsDirection.After,
                SequenceMatcherInsertionResultingBounds.ExcludingInsertion,
                CodeInstruction.LoadArgument(1), // g
                CodeInstruction.LoadLocal(14), // pos
                CodeInstruction.Call((G g, Vec pos) => AddCharLockedTooltip(g, pos))
            );
        
        return seqMatched.AllElements();
    }

    public static void AddCharLockedTooltip(G g, Vec pos)
    {
        g.tooltips.AddText(pos, ModEntry.Instance.Localizations.Localize(["newRunOptions", "charUnlock"]));
    }
}

// Ensure we don't have unlocked characters we shouldn't have everytime we open the NewRunOptions screen
[HarmonyPatch(typeof(NewRunOptions), nameof(NewRunOptions.OnEnter))]
public class NewRunOptionsEnterPatch
{
    public static void Postfix(State s)
    {
        UnlockCharPatch.RewriteUnlockedCharsFromAP(s.storyVars);
        UnlockShipPatch.RewriteUnlockedShipsFromAP(s.storyVars);
    }
}

[HarmonyPatch(typeof(NewRunOptions), nameof(NewRunOptions.Render))]
public class NewRunOptionsRenderPatch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> storedInstructions = new(instructions);

        var nextSectionLabel = generator.DefineLabel();

        // Using Shrike here because I don't know how to use indices with CodeMatcher
        var seqMatched = new SequenceBlockMatcher<CodeInstruction>(storedInstructions)
            // Replace ship unlock text
            .Find(
                ILMatches.Stloc(15),
                ILMatches.Ldloc(15)  // Outside of flag checks
            )
            // We add our code inside a ship unlock check so that it only appears if the ship ISN'T unlocked
            .PointerMatcher(SequenceMatcherRelativeElement.Last)
            .Insert(
                SequenceMatcherPastBoundsDirection.After,
                SequenceMatcherInsertionResultingBounds.JustInsertion,
                new CodeInstruction(OpCodes.Pop),            // Cancel previous ldloc without removing its labels
                CodeInstruction.LoadLocal(14),                              // Ship unlocked flag
                new CodeInstruction(OpCodes.Brtrue, nextSectionLabel),    // Don't do anything if the ship is unlocked
                CodeInstruction.Call(typeof(NewRunOptionsRenderPatch), nameof(ShipLockedText)),
                CodeInstruction.StoreLocal(15),                                  // Replace the local string with our text
                CodeInstruction.LoadLocal(15).WithLabels(nextSectionLabel)  // Becomes entrypoint for next section
            );
        
        return seqMatched.AllElements();
    }

    public static string ShipLockedText() => ModEntry.Instance.Localizations.Localize(["newRunOptions", "shipUnlock"]);
}