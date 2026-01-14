using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

    internal static Spr ArtCommon;
    internal static Spr ArtUncommon;
    internal static Spr ArtRare;
    
    public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
    {
        RegisterWithRarity(package, helper, Rarity.common, typeof(CheckLocationCard));
    }

    internal static void RegisterWithRarity(IPluginPackage<IModManifest> package, IModHelper helper, Rarity rarity, Type cardType)
    {
        helper.Content.Cards.RegisterCard(new CardConfiguration
        {
            CardType = cardType,
            Meta = new CardMeta
            {
                deck = ModEntry.Instance.ArchipelagoDeck.Deck,
                rarity = rarity,
                upgradesTo = [Upgrade.A, Upgrade.B]
            },
            Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "CheckLocationCard", "name"]).Localize,
        });
    }

    public override List<CardAction> GetActions(State s, Combat c)
    {
        Debug.Assert(Archipelago.Instance.Session != null, "Archipelago.Instance.Session != null");
        var checkAction = new AArchipelagoCheckLocation
        {
            locationName = locationName
        };
        if (locationItemName is not null)
        {
            checkAction.itemName = locationItemName;
            checkAction.receiverName = locationSlotName;
            if (IsLocal())
            {
                if (Archipelago.ItemToCard.ContainsKey(locationItemName))
                    checkAction.givenCard = locationItemName;
                else if (Archipelago.ItemToArtifact.ContainsKey(locationItemName))
                    checkAction.givenArtifact = locationItemName;
            }
        }
        
        var list = new List<CardAction> { checkAction };
        
        if (Difficulty < 0)
            list.Add(new ADrawCard
            {
                count = GetDraw(s)
            });

        switch (upgrade)
        {
            case Upgrade.A:
                list.Add(new AStatus
                {
                    status = IsShieldTemp(s) ? Status.tempShield : Status.shield,
                    statusAmount = GetShield(s),
                    mode = AStatusMode.Add,
                    targetPlayer = true
                });
                break;
            case Upgrade.B:
                var attack = new AAttack { damage = GetAttack(s) };
                for (var i = 0; i < GetAttackTimes(s); i++)
                    list.Add(attack);
                break;
        }
        
        return list;
    }

    private static string Localize(params string[] key) =>
        ModEntry.Instance.Localizations.Localize(new List<string> { "card", "CheckLocationCard" }
                                                     .Concat(key).ToArray());

    public override CardData GetData(State state)
    {
        string description;
        if (locationSlotName is null || locationItemName is null)
        {
            description = Localize("descNotFound");
        }
        else
        {
            if (IsLocal())
            {
                description = Localize(WillAddCardToDeck(state)
                                           ? "descSelfAddCard"
                                           : WillAddArtifact(state)
                                               ? "descSelfAddArtifact"
                                               : "descSelf");
                description = string.Format(description, locationItemName);
            }
            else
            {
                // Truncate the text to fit in the card
                var varTextLength = locationItemName.Length + locationSlotName.Length;
                var charsToRemove = varTextLength - 28;
                var effItemName = locationItemName;
                if (charsToRemove > 0)
                    effItemName = effItemName.Remove(Math.Max(effItemName.Length - charsToRemove, 0)) + "...";

                description = Localize("descBase");
                description = string.Format(description, effItemName, locationSlotName);
            }
        }

        if (Difficulty < 0)
        {
            description += Localize("descDraw");
            description = string.Format(description, GetDraw(state));
        }
        
        switch (upgrade)
        {
            case Upgrade.A:
                description += Localize(IsShieldTemp(state) ? "descContTempShield" : "descContShield");
                description = string.Format(description, GetShield(state));
                break;
            case Upgrade.B:
                description += Localize("descContAttack");
                description = string.Format(description, GetAttack(state), GetAttackTimes(state));
                break;
        }
        
        return new CardData
        {
            cost = GetCost(state),
            singleUse = true,
            description = description,
            art = this switch
            {
                CheckLocationCardUncommon => ArtUncommon,
                CheckLocationCardRare => ArtRare,
                _ => ArtCommon
            },
            artTint = "CCCCCC"
        };
    }

    private static int Difficulty => Archipelago.InstanceSlotData.CheckCardDifficulty;

    private int GetCost(State _)
    {
        return upgrade switch
        {
            Upgrade.A => Math.Max(0, Difficulty - 2),
            Upgrade.B => Math.Max(0, Difficulty - 1),
            _ => Difficulty
        };
    }

    private int GetShield(State _) => Difficulty <= 2 ? 2 : 3;

    private bool IsShieldTemp(State _) => Difficulty <= 3;
    
    private int GetAttack(State s) => GetDmg(s, Difficulty switch
    {
        <= 1 => 1,
        2 => 2,
        _ => 3
    });

    private int GetAttackTimes(State _) => Difficulty <= 3 ? 2 : 3;

    private int GetDraw(State _) => 1;
    
    private bool IsLocal()
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        return locationSlotName == Archipelago.Instance.APSaveData.Slot;
    }

    private bool HasDeck(State state) =>
        locationItemName is not null
        && (
            (DB.cardMetas.TryGetValue(Archipelago.ItemToCard[locationItemName].Name, out var cardMeta)
             && state.characters.Any(character => character.deckType == cardMeta.deck))
            || (DB.artifactMetas.TryGetValue(Archipelago.ItemToArtifact[locationItemName].Name, out var artifactMeta)
                && state.characters.Any(character => character.deckType == artifactMeta.owner))
        );

    private bool WillAddCardToDeck(State state)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        if (locationItemName is null) return false;
        if (!IsLocal()) return false;
        if (!Archipelago.ItemToCard.ContainsKey(locationItemName)) return false;
        if (Archipelago.Instance.APSaveData.HasItem(locationItemName)) return false;
        return Archipelago.InstanceSlotData.ImmediateCardRewards switch
        {
            CardRewardsMode.Always or CardRewardsMode.IfLocal => true,
            CardRewardsMode.IfHasDeck or CardRewardsMode.IfLocalAndHasDeck => HasDeck(state),
            _ => false
        };
    }

    private bool WillAddArtifact(State state)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        if (locationItemName is null) return false;
        if (!IsLocal()) return false;
        if (!Archipelago.ItemToArtifact.ContainsKey(locationItemName)) return false;
        if (Archipelago.Instance.APSaveData.HasItem(locationItemName)) return false;
        return Archipelago.InstanceSlotData.ImmediateArtifactRewards switch
        {
            CardRewardsMode.Always or CardRewardsMode.IfLocal => true,
            CardRewardsMode.IfHasDeck or CardRewardsMode.IfLocalAndHasDeck => HasDeck(state),
            _ => false
        };
    }

    internal void SetTextInfo(string itemName, string slotName)
    {
        locationItemName = itemName;
        locationSlotName = slotName;
    }
}

public class CheckLocationCardUncommon : CheckLocationCard
{
    public new static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
    {
        RegisterWithRarity(package, helper, Rarity.uncommon, typeof(CheckLocationCardUncommon));
    }
}

public class CheckLocationCardRare : CheckLocationCard
{
    public new static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
    {
        RegisterWithRarity(package, helper, Rarity.rare, typeof(CheckLocationCardRare));
    }
}
