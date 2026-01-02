using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using CobaltCoreArchipelago.Actions;
using Nanoray.PluginManager;
using Nickel;

namespace CobaltCoreArchipelago.Cards;

public class CheckLocationCard : Card, IRegisterable
{
    public string locationName = "";
    public string? locationSlotName;
    public string? locationItemName;

    private const int Shield = 3;
    private const int Damage = 2;
    private const int DamageTimes = 3;
    
    public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
    {
        helper.Content.Cards.RegisterCard(new CardConfiguration
        {
            CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
            Meta = new CardMeta
            {
                deck = ModEntry.Instance.ArchipelagoDeck.Deck,
                rarity = Rarity.common,
                upgradesTo = [Upgrade.A, Upgrade.B]
            },
            Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "CheckLocationCard", "name"]).Localize,
        });
    }

    public override List<CardAction> GetActions(State s, Combat c)
    {
        Debug.Assert(Archipelago.Instance.Session != null, "Archipelago.Instance.Session != null");
        var list = new List<CardAction>
        {
            new AArchipelagoCheckLocation { locationName = locationName }
        };

        switch (upgrade)
        {
            case Upgrade.A:
                list.Add(new AStatus
                {
                    status = Status.shield,
                    statusAmount = Shield,
                    mode = AStatusMode.Add,
                    targetPlayer = true
                });
                break;
            case Upgrade.B:
                var attack = new AAttack { damage = GetDmg(s, Damage) };
                for (int i = 0; i < DamageTimes; i++)
                    list.Add(attack);
                break;
        }
        
        return list;
    }

    public override CardData GetData(State state)
    {
        string description;
        if (locationSlotName is null)
        {
            description = ModEntry.Instance.Localizations.Localize(
                ["card", "CheckLocationCard", "descNotFound"]);
        }
        else
        {
            if (IsLocal())
            {
                description = ModEntry.Instance.Localizations.Localize(
                    ["card", "CheckLocationCard", "descSelf"]);
                description = string.Format(description, locationItemName);
            }
            else
            {
                description = ModEntry.Instance.Localizations.Localize(
                    ["card", "CheckLocationCard", "descBase"]);
                description = string.Format(description, locationItemName, locationSlotName);
            }
        }
        
        switch (upgrade)
        {
            case Upgrade.A:
                description += ModEntry.Instance.Localizations.Localize(
                    ["card", "CheckLocationCard", "descContA"]);
                description = string.Format(description, Shield);
                break;
            case Upgrade.B:
                description += ModEntry.Instance.Localizations.Localize(
                    ["card", "CheckLocationCard", "descContB"]);
                description = string.Format(description, GetDmg(state, Damage), DamageTimes);
                break;
        }
        
        return new CardData
        {
            cost = upgrade == Upgrade.A ? 0 : 1,
            singleUse = true,
            description = description
        };
    }
    
    private bool IsLocal()
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        return locationItemName == Archipelago.Instance.APSaveData.Slot;
    }

    internal void ScoutTextInfo()
    {
        Archipelago.Instance.CheckLocationInfo(locationName).ContinueWith(task =>
        {
            var (itemName, slotName) = task.Result;
            locationItemName = itemName;
            locationSlotName = slotName;
        });
    }
}