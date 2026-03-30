using HarmonyLib;

namespace CobaltCoreArchipelago.Features;

internal class RouteOverlay
{
    private APShout? currentShout;
    
    internal static RouteOverlay MakeNew()
    {
        StateRenderPostfix.overlay = new RouteOverlay();
        return StateRenderPostfix.overlay;
    }

    internal static void Remove()
    {
        StateRenderPostfix.overlay = null;
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
                x: 94.0, y: 20.0,
                dir: BlurbDir.Right,
                align: -0.4,
                progress: currentShout.progress,
                textColor: Colors.textBold,
                borderColor: DB.decks[Deck.colorless].color,
                maxWidth: 200.0,
                showStem: g.metaRoute is null && (s.routeOverride is null || s.routeOverride.GetShowCockpit())
            );
        }
    }
}

[HarmonyPatch(typeof(State), nameof(State.Render))]
public static class StateRenderPostfix
{
    internal static RouteOverlay? overlay;
    
    public static void Postfix(State __instance, G g)
    {
        overlay?.Render(g, __instance);
    }
}
