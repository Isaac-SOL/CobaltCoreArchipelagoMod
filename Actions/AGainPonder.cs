using System.Collections.Generic;
using System.Linq;
using CobaltCoreArchipelago.Artifacts;
using CobaltCoreArchipelago.Cards;
using Nickel;

namespace CobaltCoreArchipelago.Actions;

/*
 * There are times where custom activity must be separated away into an action. The reasons to do so include:
 * Complex behavior, which would be too verbose (take too many actions) to achieve with regular actions.
 * New behavior, such as adding ship parts.
 * Centralizing even mildly complex behavior, such as what happens here, applying Lexicon's alternation.
 */
public class AGainPonder : CardAction
{
    public static Spr DrawSpr;
    public static Spr DiscardSpr;
    
    public int Count;
    public CardDestination Destination;

    /*
     * Actions have two "perform your action" methods - Begin and BeginWithRoute.
     * Begin is for when you simply wish to perform your action, without changing the screen.
     * BeginWithRoute is for when you may wish to switch the screen, such as for card selections.
     */
    public override void Begin(G g, State s, Combat c)
    {
        c.QueueImmediate(Enumerable.Repeat<CardAction?>(null, Count)
            .Select(_ => new AAddCard
            {
                destination = Destination,
                card = new Ponder
                {
                    upgrade = GetNextUpgrade(s)
                }
            }));
    }

    /*
     * Actions can also be given their own icons, allowing for you to place a more descriptive icon, number, and even color.
     * Note that if you wish to include a number, you must also declare its color.
     */
    public override Icon? GetIcon(State s)
    {
        return new Icon
        {
            path = Destination == CardDestination.Deck ? DrawSpr : DiscardSpr,
            number = Count,
            color = Colors.textMain
        };
    }

    /*
     * Actions can also declare their own tooltips, bringing up relevant information.
     * When seeing a card's tooltips, all the actions' tooltips are tallied, to avoid repeats.
     */
    public override List<Tooltip> GetTooltips(State s)
    {
        var side = Destination == CardDestination.Discard ? "Discard" : "Draw";
        return
        [
            /*
             * GlossaryTooltip allows you to add arbitrary tooltips.
             * The constructor calls for a key, which is used for preventing duplicates on things with many, such as cards.
             * Icon is the sprite next to the title.
             * IconColor exists to tint the sprite, but is not used here.
             * Title is the text placed on top, next to the Icon.
             * TitleColor tints the title's text, otherwise it will be the default color.
             * Description is the body of the tooltip.
             * In addition, there is IsWideIcon (for 18 px icons), FlipIconX, and FlipIconY (for flipping the sprite)
             */
            new GlossaryTooltip($"AGainPonder::{side}")
            {
                Icon = Destination == CardDestination.Deck ? DrawSpr : DiscardSpr,
                Title = ModEntry.Instance.Localizations.Localize(["action", "AGainPonder", "title"]),
                TitleColor = Colors.card,
                Description = ModEntry.Instance.Localizations.Localize(["action", "AGainPonder", side.ToLower()], this)
            },
            /*
             * TTCard adds a card to the tooltip.
             */
            new TTCard
            {
                card = new Ponder()
            }
        ];
    }

    private static Upgrade GetNextUpgrade(State s)
    {
        if (s.EnumerateAllArtifacts().Find(a => a is Lexicon) is not Lexicon lexicon)
            return Upgrade.None;
        return lexicon.PullAndFlip();
    }
}