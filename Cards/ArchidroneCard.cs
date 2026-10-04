using System.Collections.Generic;
using Nanoray.PluginManager;
using Nickel;

namespace CobaltCoreArchipelago.Cards;

public class ArchidroneCard : Card, IRegisterable
{
    public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
    {
        helper.Content.Cards.RegisterCard(new CardConfiguration
        {
            CardType = typeof(ArchidroneCard),
            Meta = new CardMeta
            {
                deck = ModEntry.Instance.ArchipelagoDeck.Deck,
                rarity = Rarity.rare,
                upgradesTo = [Upgrade.A, Upgrade.B]
            },
            Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Archidrone", "cardName"]).Localize
        });
    }

    public override List<CardAction> GetActions(State s, Combat c) =>
    [
        new ASpawn
        {
            thing = new Archidrone
            {
                bubbleShield = upgrade == Upgrade.A
            },
            isaacNamesIt = false
        }
    ];

    private int GetCost() => upgrade switch
    {
        Upgrade.B => Archiprism.totalPlayers switch
        {
            // Doesn't appear below 2
            <= 3 => 3,
            <= 6 => 4,
            _ => 5
            // Doesn't appear above 10
        },
        _ => Archiprism.totalPlayers switch
        {
            // Doesn't appear below 2
            <= 3 => 2,
            <= 6 => 3,
            _ => 4
            // Doesn't appear above 10
        }
    };

    public override CardData GetData(State state) =>
        new()
        {
            art = StableSpr.cards_GoatDrone,
            cost = GetCost(),
            exhaust = upgrade != Upgrade.B,
            artTint = Colors.boldPink.ToString()
        };
}