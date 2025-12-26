using System.Collections.Generic;
using CobaltCoreArchipelago.Cards;
using Nickel;

namespace CobaltCoreArchipelago.Actions;

public class AOverthink : CardAction
{
    public static Spr Spr;

    public override void Begin(G g, State s, Combat c)
    {
        Card? target = null;

        foreach (var card in c.hand)
        {
            if (card is not Ponder) continue;
            target = card;
            break;
        }

        if (target == null)
        {
            foreach (var card in s.deck)
            {
                if (card is not Ponder) continue;
                target = card;
                break;
            }
        }

        if (target == null) return;
        s.RemoveCardFromWhereverItIs(target.uuid);
        c.SendCardToDiscard(s, target);
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
            new GlossaryTooltip($"AOverthink")
            {
                Icon = Spr,
                Title = ModEntry.Instance.Localizations.Localize(["action", "AOverthink", "title"]),
                TitleColor = Colors.card,
                Description = ModEntry.Instance.Localizations.Localize(["action", "AOverthink", "desc"])
            },
            new TTCard
            {
                card = new Ponder()
            }
        ];
    }
}