using System.Collections.Generic;
using System.Linq;
using Nickel;

namespace CobaltCoreArchipelago.Actions;

public class AArchiprismTooltip : CardAction
{
    public required HashSet<string> playersContributing;
    public required int attack;
    public int attackTimes;
    public bool upgradeB = false;
    
    public AArchiprismTooltip()
    {
        timer = 0.0;
    }
    
    public override List<Tooltip> GetTooltips(State s)
    {
        var allPlayers = playersContributing.Count == 0
            ? ""
            : playersContributing
                .Select(p => $"<c={APColors.FromPlayerName(p)}>{p}</c>")
                .Aggregate((s1, s2) => $"{s1}, {s2}");
        return
        [
            new GlossaryTooltip("AArchiprismTooltip")
            {
                Icon = AArchipelagoCheckLocation.Spr,
                Title = ModEntry.Instance.Localizations.Localize(["action", "AArchiprismTooltip", "title"]),
                TitleColor = Colors.boldPink,
                Description = upgradeB
                    ? string.Format(
                        ModEntry.Instance.Localizations.Localize(["action", "AArchiprismTooltip", "contributingB"]),
                        attack, attackTimes, allPlayers
                    )
                    : string.Format(
                        ModEntry.Instance.Localizations.Localize(["action", "AArchiprismTooltip", "contributing"]),
                        attack, allPlayers
                    )
            },
            new TTDivider()
        ];
    }

    public override Icon? GetIcon(State s)
    {
        return new Icon
        {
            path = AArchipelagoCheckLocation.Spr
        };
    }
}