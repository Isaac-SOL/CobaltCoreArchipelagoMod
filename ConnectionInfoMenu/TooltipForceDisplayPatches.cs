using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;

namespace CobaltCoreArchipelago.ConnectionInfoMenu;

// Render tooltips even during connection
[HarmonyPatch(typeof(G), nameof(G.Render))]
public class ConnectionInfoTooltipPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator _)
    {
        List<CodeInstruction> storedInstructions = new(instructions);

        // Allow displaying labels in ConnectionInfoMenu, even if state is null
        var seqMatched = new SequenceBlockMatcher<CodeInstruction>(storedInstructions)
            .Find(
                ILMatches.Brfalse,
                ILMatches.LdcI4(0),
                ILMatches.Stloc(9),
                ILMatches.Ldloc(9).ExtractLabels(out var stolenLabels)
            )
            .PointerMatcher(SequenceMatcherRelativeElement.Last)
            .Insert(
                SequenceMatcherPastBoundsDirection.Before,
                SequenceMatcherInsertionResultingBounds.JustInsertion,
                [
                    CodeInstruction.LoadArgument(0).WithLabels(stolenLabels),
                    CodeInstruction.LoadLocal(9),
                    CodeInstruction.Call((G g, bool flag) => InConnectionInfo(g, flag)),
                    CodeInstruction.StoreLocal(9)
                ]);
        
        return seqMatched.AllElements();
    }

    public static bool InConnectionInfo(G g, bool flag) =>
        flag || g.metaRoute?.subRoute is ConnectionInfoInput;
}

[HarmonyPatch(typeof(Tooltips), nameof(Tooltips.Render))]
public class TooltipRenderCheckStatePatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> storedInstructions = new(instructions);

        // Check that state is not null before displaying (state is being used if we use a controller)
        // This is necessary because of the above G.Render patch
        var seqMatched = new SequenceBlockMatcher<CodeInstruction>(storedInstructions)
            .Find(
                ILMatches.Br,
                ILMatches.LdcI4(0),
                ILMatches.Stloc(1),
                ILMatches.Br,
                ILMatches.Ldarg(0).CreateLabel(generator, out var ifFailLabel)
            )
            .Find(
                SequenceBlockMatcherFindOccurence.First,
                SequenceMatcherRelativeBounds.Before,
                ILMatches.Ldsfld<bool>(),
                ILMatches.Brfalse)
            .PointerMatcher(SequenceMatcherRelativeElement.Last)
            .Insert(
                SequenceMatcherPastBoundsDirection.After,
                SequenceMatcherInsertionResultingBounds.JustInsertion,
                [
                    CodeInstruction.LoadArgument(1),
                    CodeInstruction.Call((G g) => IsStateValid(g)),
                    new CodeInstruction(OpCodes.Brfalse, ifFailLabel)
                ]);
        
        return seqMatched.AllElements();
    }
    
    // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
    public static bool IsStateValid(G g) => g.state != null;
}