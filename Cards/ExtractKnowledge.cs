using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace CobaltCoreArchipelago.Cards;

public class ExtractKnowledge : Card, IRegisterable
{
    public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
    {
        helper.Content.Cards.RegisterCard(new CardConfiguration
        {
            CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
            Meta = new CardMeta
            {
                deck = ModEntry.Instance.DemoDeck.Deck,
                rarity = Rarity.rare,
                upgradesTo = [Upgrade.A, Upgrade.B]
            },
            Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "ExtractKnowledge", "name"]).Localize,
            // Art = ModEntry.RegisterSprite(package, "assets/Cards/Ponder.png").Sprite
        });
    }

    public override List<CardAction> GetActions(State s, Combat c)
    {
        var shot = new AAttack
        {
            damage = Damage(s),
            onKillActions = [
                new AAddCard
                {
                    card = new Ponder
                    {
                        /*
                         * All Overrides default to null, indicating they are not overriding anything.
                         * Temporary = false makes an overwise-temporary card permanent.
                         */
                        temporaryOverride = false
                    }
                }
            ]
        };

        if (upgrade == Upgrade.B)
        {
            shot.onKillActions =
            [
                new AAddCard
                {
                    card = new Ponder
                    {
                        temporaryOverride = false,
                        upgrade = Upgrade.B
                    }
                }
            ];
        }

        return [shot];
    }

    public override CardData GetData(State state)
    {
        return new CardData
        {
            cost = 3,
            exhaust = true,
            description = string.Format(ModEntry.Instance.Localizations.Localize([
                "card",
                "ExtractKnowledge",
                upgrade == Upgrade.B ? "descB" : "desc"]
                ), Damage(state))
        };
    }

    private int Damage(State s)
    {
        return GetDmg(s, upgrade == Upgrade.A ? 3 : 0);
    }
}