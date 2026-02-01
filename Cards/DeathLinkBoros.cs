using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Nanoray.PluginManager;
using Nickel;

namespace CobaltCoreArchipelago.Cards;

public class DeathLinkBoros : Card, IRegisterable
{
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

    private int GetHullDamageFactor() => Archipelago.Instance.APSaveData!.DeathLinkHullDamage switch
    {
        <= 1 => 5,
        <= 2 => 4,
        <= 4 => 3,
        <= 8 => 2,
        _ => 1
    };

    private int GetHullDamagePercentFactor() => Archipelago.Instance.APSaveData!.DeathLinkHullDamagePercent switch
    {
        <= 10 => 5,
        <= 20 => 4,
        <= 40 => 3,
        <= 80 => 2,
        _ => 1
    };

    private int GetDeathLinkStep() => (upgrade, Archipelago.Instance.APSaveData!.DeathLinkMode) switch
    {
        (Upgrade.A,    DeathLinkMode.Missing) => 8,
        (Upgrade.B,    DeathLinkMode.Missing) => 16,
        (Upgrade.None, DeathLinkMode.Missing) => 8,

        (Upgrade.A,    DeathLinkMode.HullDamage) => GetHullDamageFactor(),
        (Upgrade.B,    DeathLinkMode.HullDamage) => GetHullDamageFactor() * 2,
        (Upgrade.None, DeathLinkMode.HullDamage) => GetHullDamageFactor(),

        (Upgrade.A,    DeathLinkMode.HullDamagePercent) => GetHullDamagePercentFactor(),
        (Upgrade.B,    DeathLinkMode.HullDamagePercent) => GetHullDamagePercentFactor() * 2,
        (Upgrade.None, DeathLinkMode.HullDamagePercent) => GetHullDamagePercentFactor(),

        (Upgrade.A,    DeathLinkMode.Death) => 1,
        (Upgrade.B,    DeathLinkMode.Death) => 2,
        (Upgrade.None, DeathLinkMode.Death) => 1,

        _ => 10
    };

    private (int step, int value, int remainder) GetDeathLinkInfo()
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        var deathLinks = Archipelago.Instance.APSaveData.DeathLinkCount;
        var step = GetDeathLinkStep();
        var value = deathLinks / step;
        var remainder = deathLinks % step;
        return (step, value, remainder);
    }

    public override List<CardAction> GetActions(State s, Combat c)
    {
        var (_, value, _) = GetDeathLinkInfo();
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
                },
                new AStatus
                {
                    status = Status.tempShield,
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
                    damage = GetDmg(s, value)
                }
            ]
        };
    }

    private static string Localize(params string[] key) =>
        ModEntry.Instance.Localizations.Localize(new List<string> { "card", "DeathLinkBoros" }
                                                     .Concat(key).ToArray());

    public override CardData GetData(State state)
    {
        var (step, value, remainder) = GetDeathLinkInfo();
        var description = string.Format(upgrade switch
        {
            Upgrade.A => Localize("descA"),
            Upgrade.B => Localize("descB"),
            _ => Localize("descBase")
        }, upgrade == Upgrade.None ? GetDmg(state, value) : value);
        description += step == 1
            ? Localize("descRampOne")
            : string.Format(Localize("descRamp"), remainder, step);
        return new CardData
        {
            cost = 2,
            description = description
        };
    }
}