using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace CobaltCoreArchipelago.Cards;

public class DeepStudy : Card, IRegisterable
{
    public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
    {
        helper.Content.Cards.RegisterCard(new CardConfiguration
        {
            CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
            Meta = new CardMeta
            {
                deck = ModEntry.Instance.DemoDeck.Deck,
                rarity = Rarity.uncommon,
                upgradesTo = [Upgrade.A, Upgrade.B]
            },
            Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "DeepStudy", "name"]).Localize,
            // Art = ModEntry.RegisterSprite(package, "assets/Cards/Ponder.png").Sprite
        });
    }

    /*
     * Occasionally, the list of actions a card has is treated in the most general regard - it is a list of actions.
     */
    public override List<CardAction> GetActions(State s, Combat c)
    {
        var lesson = s.ship.Get(ModEntry.Instance.LessonStatus.Status);
        var knowledge = upgrade == Upgrade.A ? 5 : 3;
        List<CardAction> actions =
        [
            /*
             * AVariableHint is the "X = something + something else" line item.
             * Technically speaking, it does nothing.
             * However, it still serves as an excellent visual aide, allowing some cards to be expressed easier than with text.
             */
            new AVariableHint
            {
                status = ModEntry.Instance.LessonStatus.Status
            },
            new AStatus
            {
                status = ModEntry.Instance.KnowledgeStatus.Status,
                targetPlayer = true,
                statusAmount = lesson,
                /*
                 * xHint replaces the number displayed by the icon, by X.
                 * It is a number to allow for things such as Hand Cannon B's 2X.
                 */
                xHint = 1
            },
            new AStatus
            {
                status = ModEntry.Instance.KnowledgeStatus.Status,
                targetPlayer = true,
                statusAmount = knowledge
            }
        ];

        if (upgrade != Upgrade.B)
        {
            actions.Add(new AEndTurn());
        }

        return actions;
    }

    public override CardData GetData(State state)
    {
        return new CardData
        {
            cost = 2,
            exhaust = true
        };
    }
}