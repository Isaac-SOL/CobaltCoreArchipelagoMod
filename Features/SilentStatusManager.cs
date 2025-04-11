using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;

namespace DemoMod.Features;

public class SilentStatusManager
{
    public SilentStatusManager()
    {
        ModEntry.Instance.Harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(AStatus), nameof(AStatus.Begin)),
            transpiler: new HarmonyMethod(GetType(), nameof(AStatus_Begin_Transpiler))
        );
    }

    /*
     * Harmony Transpilers allow you to modify the body of an existing method.
     * They are typically harder to write than prefixes and postfixes, as you are working on the individual instruction level.
     * Shrike's SequenceBlockMatcher makes the necessary instruction replacements considerably easier, compared to
     * inspecting the input enumerable manually.
     */
    public static IEnumerable<CodeInstruction> AStatus_Begin_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        try
        {
            return new SequenceBlockMatcher<CodeInstruction>(instructions)
                .Find(SequenceBlockMatcherFindOccurence.Last,
                    SequenceMatcherRelativeBounds.Enclosed,
                    ILMatches.Instruction(OpCodes.Ret).CreateLabel(il, out var ret))
                .Find(SequenceBlockMatcherFindOccurence.Last,
                    SequenceMatcherRelativeBounds.WholeSequence,
                    ILMatches.Ldarg(0).ExtractLabels(out var labels))
                .Insert(SequenceMatcherPastBoundsDirection.Before,
                    SequenceMatcherInsertionResultingBounds.JustInsertion,
                    [
                        new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
                        new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(SilentStatusManager), "ShouldSilence")),
                        new(OpCodes.Brtrue, ret)
                    ])
                .AllElements();
        }
        catch (Exception e)
        {
            Console.WriteLine("SilentStatusManager's AStatus_Begin_Transpiler failed");
            Console.WriteLine(e);
            throw;
        }
    }

    public static bool ShouldSilence(AStatus status)
    {
        return ModEntry.Instance.Helper.ModData.TryGetModData<bool>(status, "Silent", out var data) && data;
    }
}

public static class AStatusExtension
{
    public static AStatus Silent(this AStatus status)
    {
        ModEntry.Instance.Helper.ModData.SetModData(status, "Silent", true);
        return status;
    }
}