using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using CobaltCoreArchipelago.Actions;
using Nanoray.PluginManager;
using Nickel;

namespace CobaltCoreArchipelago.Cards;

public class Archiprism : Card, IRegisterable
{
    internal static int totalPlayers;
    
    public HashSet<string> playersContributing = [];
    public HashSet<string> playersContributingProgression = [];
    public HashSet<string> playersContributingTrap = [];

    private HashSet<string> EffPlayersContributing => upgrade switch
    {
        Upgrade.A => playersContributingProgression,
        Upgrade.B => playersContributingTrap,
        _ => playersContributing
    };
    
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
            Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Archiprism", "name"]).Localize,
        });
    }

    public void PlayerFoundItem(string player, ItemFlags flags)
    {
        if (player == "Server") return;
        playersContributing.Add(player);
        if ((flags & ItemFlags.Advancement) != 0) playersContributingProgression.Add(player);
        if ((flags & ItemFlags.Trap) != 0) playersContributingTrap.Add(player);
    }

    public override void AfterWasPlayed(State state, Combat c)
    {
        playersContributing.Clear();
        playersContributingProgression.Clear();
        playersContributingTrap.Clear();
    }

    public override List<CardAction> GetActions(State s, Combat c)
    {
        List<CardAction> actions =
        [
            new AArchiprismTooltip
            {
                playersContributing = EffPlayersContributing,
                attack = upgrade == Upgrade.B ? GetActualOnePlayerDmg(s) : GetPrismDmg(s),
                attackTimes = EffPlayersContributing.Count,
                upgradeB = upgrade == Upgrade.B
            }
        ];
        
        if (upgrade == Upgrade.B)
        {
            for (var i = 0; i < EffPlayersContributing.Count; i++)
            {
                actions.Add(new AAttack
                {
                    damage = GetActualOnePlayerDmg(s)
                });
            }
        }
        else
        {
            actions.Add(new AAttack
            {
                damage = GetPrismDmg(s)
            });
        }

        actions.Add(new AInvalidateUndos
        {
            type = InvalidationTypes.ArchiprismAttack
        });

        return actions;
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
    
    private int GetOnePlayerDmg() => upgrade switch
    {
        Upgrade.A => 3,
        Upgrade.B => 5,
        _ => 1
    };

    private int GetActualOnePlayerDmg(State s) => GetActualDamage(s, GetOnePlayerDmg());

    private int GetPrismDmg(State s) => GetActualDamage(s, EffPlayersContributing.Count * GetOnePlayerDmg());

    public override CardData GetData(State state)
    {
        var description = ModEntry.Instance.Localizations.Localize(upgrade switch
        {
            Upgrade.A => ["card", "Archiprism", "descA"],
            Upgrade.B => ["card", "Archiprism", "descB"],
            _ => ["card", "Archiprism", "desc"]
        });
        description = string.Format(description, GetOnePlayerDmg());

        description += state.route is CardBrowse { browseSource: CardBrowse.Source.Codex } ? "."
            : upgrade != Upgrade.B ? $": <c=attack>{GetPrismDmg(state)}</c>."
            : $": <c=attack>{GetActualOnePlayerDmg(state)}</c>x{EffPlayersContributing.Count}.";

        return new CardData
        {
            art = StableSpr.cards_Prism,
            cost = GetCost(),
            description = description,
            exhaust = upgrade == Upgrade.B,
            artTint = Colors.white.ToString()
        };
    }
}