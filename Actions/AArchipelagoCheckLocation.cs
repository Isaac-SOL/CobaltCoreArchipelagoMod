using System.Collections.Generic;
using System.Diagnostics;
using CobaltCoreArchipelago.Cards;
using Nickel;

namespace CobaltCoreArchipelago.Actions;

public class AArchipelagoCheckLocation : CardAction
{
    public static Spr Spr;

    public string? locationName;

    public override void Begin(G g, State s, Combat c)
    {
        Debug.Assert(locationName != null, nameof(locationName) + " != null");
        Archipelago.Instance.CheckLocation(locationName);
    }
    
    public override Icon? GetIcon(State s)
    {
        return new Icon
        {
            path = Spr
        };
    }
    
    public override List<Tooltip> GetTooltips(State s)
    {
        return
        [
            new GlossaryTooltip($"AArchipelagoCheckLocation")
            {
                Icon = Spr,
                Title = ModEntry.Instance.Localizations.Localize(["action", "AArchipelagoCheckLocation", "title"]),
                TitleColor = Colors.card,
                Description = ModEntry.Instance.Localizations.Localize(["action", "AArchipelagoCheckLocation", "desc"])
            },
            new TTCard
            {
                card = new Ponder()
            }
        ];
    }
}