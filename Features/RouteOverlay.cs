using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;

namespace CobaltCoreArchipelago.Features;

internal class RouteOverlay
{
    private APShout? currentShout;
    
    internal static RouteOverlay MakeNew()
    {
        GRenderPostfix.overlay = new RouteOverlay();
        return GRenderPostfix.overlay;
    }

    internal static void Remove()
    {
        GRenderPostfix.overlay = null;
    }
    
    internal void Render(G g, State s)
    {
        if (Archipelago.Instance.MessagesToAnnounce.Count > 0)
        {
            if (currentShout is null)
            {
                // There is a message to display and no message currently being shown
                var message = Archipelago.Instance.MessagesToAnnounce[0];
                string messageStr;
                if (message.type == MessageToAnnounce.DeathlinkReceived)
                {
                    messageStr = ModEntry.Instance.Localizations.Localize(["compShouts", "defaultDeathlink"]);
                    messageStr = string.Format(
                        messageStr,
                        $"<c={APColors.OtherPlayer}>{message.deathlink!.Source}</c>",
                        message.deathlink!.Cause
                        );
                }
                else
                {
                    messageStr = ModEntry.Instance.Localizations.Localize(["compShouts", "defaultItem"]);
                    var item = message.item!;
                    messageStr = string.Format(
                        messageStr,
                        $"<c={APColors.FromFlags(item.Flags)}>{item.ItemName}</c>",
                        $"<c={APColors.FromPlayerName(item.Player.Name)}>{item.Player.Name}</c>"
                    );
                }
                currentShout = new APShout
                {
                    message = messageStr
                };
                Archipelago.Instance.MessagesToAnnounce.RemoveAt(0);
            }
            else
            {
                // There is a message currently being shown and more to display
                currentShout.SkipToEnd();
            }
        }
        currentShout?.Update(g);
        if (currentShout is not null && currentShout.progress > currentShout.message.Length + 200.0)
            currentShout = null;
        if (currentShout is not null && currentShout.delay == 0.0)
        {
            Blurbs.Render(
                g,
                currentShout.message,
                x: 94.0 + g.cornerMenu.GetExtraOffset(), y: 20.0,
                dir: BlurbDir.Right,
                align: -0.4,
                progress: currentShout.progress,
                textColor: Colors.textBold,
                borderColor: DB.decks[Deck.colorless].color,
                maxWidth: 200.0,
                showStem: g.metaRoute is null && ((s.routeOverride is null && s.route.GetShowCockpit())
                                                  || (s.routeOverride is not null && s.routeOverride.GetShowCockpit()))
            );
        }
    }
}

[HarmonyPatch(typeof(G), nameof(G.Render))]
public static class GRenderPostfix
{
    internal static RouteOverlay? overlay;
    
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> storedInstructions = new(instructions);

        // Using Shrike here because I don't know how to use indices with CodeMatcher
        var seqMatched = new SequenceBlockMatcher<CodeInstruction>(storedInstructions)
            // Match sequence where the function adds the tooltip by checking showUnlockInstructions
            .Find(
                ILMatches.Ldarg(0),
                ILMatches.Call(nameof(G.Pop))
            )
            .Insert(
                SequenceMatcherPastBoundsDirection.After,
                SequenceMatcherInsertionResultingBounds.ExcludingInsertion,
                CodeInstruction.LoadArgument(0), // this
                CodeInstruction.LoadArgument(0),
                CodeInstruction.LoadField(typeof(G), nameof(G.state)), // this.state
                CodeInstruction.Call((G g, State s) => RenderOverlay(g, s))
            );
        
        return seqMatched.AllElements();
    }

    public static void RenderOverlay(G g, State s)
    {
        overlay?.Render(g, s);
    }
}
