using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Nanoray.PluginManager;
using Nickel;

namespace CobaltCoreArchipelago.Cards;

public class DeathLinkBoros : Card, IRegisterable
{
    private const int stepBase = 3, stepA = 3, stepB = 6;
    
    public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
    {
        helper.Content.Cards.RegisterCard(new CardConfiguration
        {
            CardType = typeof(DeathLinkBoros),
            Meta = new CardMeta
            {
                deck = ModEntry.Instance.ArchipelagoDeck.Deck,
                rarity = Rarity.rare,
                upgradesTo = [Upgrade.A, Upgrade.B]
            },
            Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "DeathLinkBoros", "name"]).Localize,
        });
    }

    private (int step, int value, int remainder) GeatDeathLinkInfo()
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        var deathLinks = Archipelago.Instance.APSaveData.DeathLinkCount;
        var step = upgrade switch
        {
            Upgrade.A => stepA,
            Upgrade.B => stepB,
            _ => stepBase
        };
        var value = deathLinks / step;
        var remainder = deathLinks % step;
        return (step, value, remainder);
    }

    public override List<CardAction> GetActions(State s, Combat c)
    {
        var (_, value, _) = GeatDeathLinkInfo();
        return upgrade switch
        {
            Upgrade.A =>
            [
                new AStatus
                {
                    status = Status.shield,
                    targetPlayer = true,
                    mode = AStatusMode.Add,
                    statusAmount = value
                }
            ],
            Upgrade.B =>
            [
                new AStatus
                {
                    status = Status.overdrive,
                    targetPlayer = true,
                    mode = AStatusMode.Add,
                    statusAmount = value
                }
            ],
            _ =>
            [
                new AAttack
                {
                    damage = value
                }
            ]
        };
    }

    private static string Localize(params string[] key) =>
        ModEntry.Instance.Localizations.Localize(new List<string> { "card", "DeathLinkBoros" }
                                                     .Concat(key).ToArray());

    public override CardData GetData(State state)
    {
        var (step, value, remainder) = GeatDeathLinkInfo();
        var description = string.Format(upgrade switch
        {
            Upgrade.A => Localize("descA"),
            Upgrade.B => Localize("descB"),
            _ => Localize("descBase")
        }, value, remainder, step);
        return new CardData
        {
            cost = 2,
            description = description
        };
    }
}