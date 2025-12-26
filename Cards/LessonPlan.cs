using System.Collections.Generic;
using System.Reflection;
using CobaltCoreArchipelago.Actions;
using Nanoray.PluginManager;
using Nickel;

namespace CobaltCoreArchipelago.Cards;

/*
 * Cards are a character's bread and butter.
 * Most characters contain 9 commons, 7 uncommons, and 5 rares, but nothing requires this distribution.
 * In addition, some cards have additional cards, which aren't offered but can still be obtained.
 * Ensure that after you create a card, that you add its type to the ModEntry's list of types to register.
 */
public class LessonPlan : Card, IRegisterable
{
    public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
    {
        helper.Content.Cards.RegisterCard(new CardConfiguration
        {
            /*
             * This is a trick to obtain the type housing the current method, to reduce the effort of making a new card.
             */
            CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
            Meta = new CardMeta
            {
                deck = ModEntry.Instance.DemoDeck.Deck,
                rarity = Rarity.common,
                dontOffer = true,
                upgradesTo = [Upgrade.A, Upgrade.B]
            },
            Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "LessonPlan", "name"]).Localize,
        });
    }


    /*
     * Each card has a list of actions, which decides both what is rendered on the card, and what actually happens on play.
     * There are many ways to construct the actions a card contains.
     * The following is a simple approach, good for if the actions on the card only vary in their numbers.
     */
    public override List<CardAction> GetActions(State s, Combat c)
    {
        return
        [
            new AGainPonder
            {
                Destination = CardDestination.Deck,
                Count = upgrade == Upgrade.B ? 5 : 3
            },
            new ADrawCard
            {
                count = upgrade == Upgrade.B ? 2 : 1
            }
        ];
    }


    /*
     * Each card also has CardData, which is things such as its cost, if it's playable, if it has exhaust, recycle, infinite, etc.
     * In addition, different arts and art tints can be enforced.
     */
    public override CardData GetData(State state)
    {
        return new CardData
        {
            cost = 1,
            exhaust = true,
            buoyant = upgrade == Upgrade.A
        };
    }
}