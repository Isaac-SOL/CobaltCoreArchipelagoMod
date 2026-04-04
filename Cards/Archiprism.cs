using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Nanoray.PluginManager;
using Nickel;

namespace CobaltCoreArchipelago.Cards;

public class Archiprism : Card, IRegisterable
{
    internal static int totalPlayers;
    
    private HashSet<string> playersContributing = [];
    
    public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
    {
        helper.Content.Cards.RegisterCard(new CardConfiguration
        {
            CardType = typeof(Archiprism),
            Meta = new CardMeta
            {
                deck = ModEntry.Instance.ArchipelagoDeck.Deck,
                rarity = Rarity.rare,
                upgradesTo = [Upgrade.A, Upgrade.B]
            },
            Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "DeathLinkBoros", "name"]).Localize,
        });
    }

    public void PlayerFoundItem(string player, ItemFlags flags)
    {
        switch (upgrade)
        {
            case Upgrade.A when (flags & ItemFlags.Advancement) == 0:
            case Upgrade.B when (flags & ItemFlags.Trap) == 0:
                return;
            case Upgrade.None:
            default:
                if (player != "Server")
                    playersContributing.Add(player);
                return;
        }
    }

    public override List<CardAction> GetActions(State s, Combat c)
    {
        return
        [
            new AAttack
            {
                damage = GetPrismDmg(s)
            }
        ];
    }

    public override void AfterWasPlayed(State state, Combat c)
    {
        playersContributing.Clear();
    }

    private static int GetCost() => totalPlayers switch
    {
        // Doesn't appear below 2
        <= 3 => 0,
        <= 4 => 1,
        <= 7 => 2,
        <= 10 => 3,
        _ => 4
        // Doesn't appear above 15
    };

    private int GetPrismDmg(State s) => GetActualDamage(s, playersContributing.Count * upgrade switch
    {
        Upgrade.A => 3,
        Upgrade.B => 5,
        _ => 1
    });

    public override CardData GetData(State state)
    {
        var description = ModEntry.Instance.Localizations.Localize(upgrade switch
        {
            Upgrade.A => ["card", "Archiprism", "descA"],
            Upgrade.B => ["card", "Archiprism", "descB"],
            _ => ["card", "Archiprism", "desc"]
        });
        description += state.route is Combat
            ? $": <c=attack>{GetPrismDmg(state)}</c>."
            : ".";

        return new CardData
        {
            art = StableSpr.cards_Prism,
            cost = GetCost(),
            description = description,
            exhaust = upgrade == Upgrade.B
        };
    }
}