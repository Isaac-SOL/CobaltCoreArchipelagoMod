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