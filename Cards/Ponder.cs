using System.Collections.Generic;
using System.Reflection;
using CobaltCoreArchipelago.Actions;
using Nanoray.PluginManager;
using Nickel;

namespace CobaltCoreArchipelago.Cards;

public class Ponder : Card, IRegisterable
{
    public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
    {
        helper.Content.Cards.RegisterCard(new CardConfiguration
        {
            CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
            Meta = new CardMeta
            {
                deck = ModEntry.Instance.DemoDeck.Deck,
                rarity = Rarity.common,
                /*
                 * Without dontOffer, this card could appear in card rewards.
                 */
                dontOffer = true,
                upgradesTo = [Upgrade.A, Upgrade.B]
            },
            Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Ponder", "name"]).Localize,
            // Art = ModEntry.RegisterSprite(package, "assets/Cards/Ponder.png").Sprite
        });
    }
    
    /*
     * This card's actions are made more specific, having a branch for the B upgrade.
     * While allowed, this approach may be inadvisable, as it leads to repetition of common parts, which may result in mistakes between upgrades and when refactoring.
     */
    public override List<CardAction> GetActions(State s, Combat c)
    {
        if (upgrade == Upgrade.B)
        {
            return
            [
                new AStatus
                {
                    /*
                     * When registering a status, you receive an IStatusEntry, which itself contains the Status.
                     * It is helpful to retain the IStatusEntry, in the event other metadata for it must be obtained.
                     */
                    status = ModEntry.Instance.KnowledgeStatus.Status,
                    statusAmount = 1,
                    targetPlayer = true
                },
                new ADrawCard
                {
                    count = 1
                }
            ];
        }
        return
        [
            new AStatus
            {
                status = ModEntry.Instance.KnowledgeStatus.Status,
                statusAmount = 1,
                targetPlayer = true
            },
            new AOverthink(),
            new ADrawCard
            {
                /*
                 * The Ternary operator (q ? t : f) is handy for simple, in-line if-else calculations.
                 * This one states that if the upgrade is Upgrade.B (q), then the count is 2 (t), else it's 1 (f).
                 */
                count = upgrade == Upgrade.A ? 2 : 1
            }
        ];
    }
    
    public override CardData GetData(State state)
    {
        return new CardData
        {
            cost = 0,
            exhaust = true,
            temporary = true
        };
    }
}