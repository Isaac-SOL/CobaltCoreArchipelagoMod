using System;
using System.Collections.Generic;
using System.Reflection;
using CobaltCoreArchipelago.External;
using Nanoray.PluginManager;
using Nickel;

namespace CobaltCoreArchipelago.Cards;

public class PatternBlock : Card, IRegisterable
{
    /*
     * This exists for the sake of brevity, as it is used a lot in this card's actions.
     */
    private static IKokoroApi.IV2.IConditionalApi Conditional => ModEntry.Instance.KokoroApi.Conditional;
    
    public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
    {
        helper.Content.Cards.RegisterCard(new CardConfiguration
        {
            CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
            Meta = new CardMeta
            {
                deck = ModEntry.Instance.DemoDeck.Deck,
                rarity = Rarity.common,
                dontOffer = true,
                upgradesTo = [Upgrade.A, Upgrade.B]
            },
            Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "PatternBlock", "name"]).Localize,
        });
    }

    /*
     * Some cards have actions that don't just differ by number on upgrade.
     * In these cases, a switch statement may be used.
     * It is more verbose, but allows for precisely describing what each upgrade's actions are.
     */
    public override List<CardAction> GetActions(State s, Combat c)
    {
        return upgrade switch
        {
            Upgrade.A => [
                new AStatus
                {
                    status = Status.shield,
                    statusAmount = 2,
                    targetPlayer = true
                },
                /*
                 * Conditional actions require two arguments:
                 * an IBooleanExpression, describing when the action should occur
                 * and a CardAction, dictating what the action is
                 */
                Conditional.MakeAction(
                    /*
                     * HasStatus will allow its action to occur if the player has any amount of the status.
                     * It has a targetPlayer argument that defaults to true, in case the query should be against the enemy.
                     * It also has a countNegative argument that defaults to false, in case negatives should also be counted.
                     */
                    Conditional.HasStatus(ModEntry.Instance.LessonStatus.Status),
                    new AStatus
                    {
                        status = Status.tempShield,
                        statusAmount = 2,
                        targetPlayer = true
                    }
                ).AsCardAction
            ],
            Upgrade.B => [
                new AStatus
                {
                    status = Status.shield,
                    statusAmount = 1,
                    targetPlayer = true
                },
                Conditional.MakeAction(
                    Conditional.HasStatus(ModEntry.Instance.LessonStatus.Status),
                    new AStatus
                    {
                        status = Status.tempShield,
                        statusAmount = 2,
                        targetPlayer = true
                    }
                ).AsCardAction,
                Conditional.MakeAction(
                    /*
                     * Equations allow for very specific conditions to occur.
                     * It takes an IIntExpression, an EquationOperator, another IIntExpression, and an EquationStyle.
                     * In the following, it is checking if the player's Lesson is greater than or equal to 2.
                     */
                    Conditional.Equation(
                        Conditional.Status(ModEntry.Instance.LessonStatus.Status),
                        IKokoroApi.IV2.IConditionalApi.EquationOperator.GreaterThanOrEqual,
                        Conditional.Constant(2),
                        IKokoroApi.IV2.IConditionalApi.EquationStyle.Formal
                    ),
                    new AStatus
                    {
                        status = Status.tempShield,
                        statusAmount = 2,
                        targetPlayer = true
                    }
                ).AsCardAction
            ],
            _ => [
                new AStatus
                {
                    status = Status.shield,
                    statusAmount = 1,
                    targetPlayer = true
                },
                Conditional.MakeAction(
                    Conditional.HasStatus(ModEntry.Instance.LessonStatus.Status),
                    new AStatus
                    {
                        status = Status.tempShield,
                        statusAmount = 2,
                        targetPlayer = true
                    }
                ).AsCardAction
            ]
        };
    }

    public override CardData GetData(State state)
    {
        return new CardData
        {
            cost = 1
        };
    }
}