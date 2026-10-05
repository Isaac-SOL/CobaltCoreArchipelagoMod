using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Archipelago.MultiClient.Net.Models;
using CobaltCoreArchipelago.Actions;
using FMOD;
using Nanoray.PluginManager;
using Nickel;
using Debug = System.Diagnostics.Debug;

namespace CobaltCoreArchipelago.Cards;

public class CheckLocationCard : Card, IRegisterable
{
    internal static Spr ArtCommon;
    internal static Spr ArtUncommon;
    internal static Spr ArtRare;
    
    // Note: fields MUST be public to be transferred when the card is upgraded or saved for example
    public string locationName = "";
    public string? locationSlotName;
    public string? locationGameName;
    public string? locationItemName;
    public string? locationItemColor;
    public Deck? locationFrom;
    
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

    private (int cost, IEnumerable<CardAction> actions, string desc) GetUpgradeData(State s)
    {
        var partialRes = (locationFrom, upgrade) switch
        {
            (_, Upgrade.None) => (Difficulty, new List<CardAction>(), ""),

            (Deck.dizzy, Upgrade.A) => (
                Difficulty - 2,
                [
                    new AStatus
                    {
                        status = IsShieldTemp(s) ? Status.tempShield : Status.shield,
                        statusAmount = GetShield(s),
                        targetPlayer = true
                    }
                ],
                string.Format(Localize("descCont", IsShieldTemp(s) ? "TempShield" : "Shield"), GetShield(s))
            ),
            (Deck.dizzy, Upgrade.B) => (
                1,
                [new ASpawn { thing = new Missile { missileType = MissileType.corrode } }],
                Localize("descCont", "AcidMissile")
            ),

            (Deck.riggs, Upgrade.A) => (Difficulty - 2, [], ""),  // Covered externally by GetDraw()
            (Deck.riggs, Upgrade.B) => (
                Difficulty - 1,
                [
                    new AStatus
                    {
                        status = Status.evade,
                        statusAmount = GetMove(s),
                        targetPlayer = true
                    }
                ],
                string.Format(Localize("descCont", "Evade"), GetMove(s))
            ),

            (Deck.peri, Upgrade.A) => (
                Difficulty > 2 ? 2 : 1,
                [
                    new AStatus
                    {
                        status = Status.overdrive,
                        statusAmount = Difficulty > 2 ? 2 : 1,
                        targetPlayer = true
                    }
                ],
                string.Format(Localize("descCont", "Overdrive"), Difficulty > 2 ? 2 : 1)
            ),
            (Deck.peri, Upgrade.B) => (
                Difficulty - 1,
                Enumerable.Repeat(new AAttack { damage = GetAttack(s) }, GetAttackTimes(s)).Cast<CardAction>(),
                string.Format(Localize("descCont", "Attack"), GetAttack(s), GetAttackTimes(s))
                ),
            
            (Deck.goat, Upgrade.A) => (
                Difficulty - 2,
                [
                    new AStatus
                    {
                        status = Status.droneShift,
                        statusAmount = GetDroneshift(s),
                        targetPlayer = true
                    }
                ],
                string.Format(Localize("descCont", "Droneshift"), GetDroneshift(s))
            ),
            (Deck.goat, Upgrade.B) => (
                Difficulty > 2 ? 2 : 1,
                [
                    new ASpawn
                    {
                        thing = new Missile { missileType = Difficulty > 2 ? MissileType.heavy : MissileType.normal }
                    }
                ],
                Localize("descCont", Difficulty > 2 ? "HeavyMissile" : "Missile")
            ),

            (Deck.eunice, Upgrade.A) => (
                1,
                [
                    new AStatus
                    {
                        status = Status.heat,
                        statusAmount = -2,
                        targetPlayer = true
                    }
                ],
                string.Format(Localize("descCont", "Heat"), -2)
            ),
            (Deck.eunice, Upgrade.B) => (
                Difficulty - 1,
                [
                    new AAttack
                    {
                        damage = GetStunAttack(s),
                        stunEnemy = true
                    }
                ],
                string.Format(Localize("descCont", "StunAttack"), GetStunAttack(s))
            ),

            (Deck.hacker, Upgrade.A) => (
                Difficulty - 2,
                [
                    new AStatus
                    {
                        status = Status.boost,
                        statusAmount = GetBoost(s),
                        targetPlayer = true
                    }
                ],
                string.Format(Localize("descCont", "Boost"), GetBoost(s))
            ),
            (Deck.hacker, Upgrade.B) => (
                2,
                [
                    new AStatus
                    {
                        status = Status.autopilot,
                        statusAmount = 2,
                        targetPlayer = true
                    }
                ],
                string.Format(Localize("descCont", "Autopilot"), 2)
            ),

            (Deck.shard, Upgrade.A) => (
                Difficulty - 2,
                [
                    new AStatus
                    {
                        status = Status.shard,
                        statusAmount = GetShard(s),
                        targetPlayer = true
                    }
                ],
                string.Format(Localize("descCont", "Shard"), GetShard(s))
            ),
            (Deck.shard, Upgrade.B) => (
                Difficulty - 2,
                [
                    new AStatus
                    {
                        status = IsShieldTemp(s) ? Status.tempShield : Status.shield,
                        statusAmount = GetShield(s),
                        targetPlayer = true
                    }
                ],
                string.Format(Localize("descCont",  IsShieldTemp(s) ? "TempShield" : "Shield"), GetShield(s))
            ),

            (Deck.colorless, Upgrade.A) => (
                Difficulty > 2 ? 2 : 1,
                [
                    new AHeal
                    {
                        healAmount = 1,
                        targetPlayer = true
                    }
                ],
                string.Format(Localize("descCont", "Heal"), 1)
            ),
            (Deck.colorless, Upgrade.B) => (
                2,
                [
                    new AStatus
                    {
                        status = Status.perfectShield,
                        statusAmount = 1,
                        targetPlayer = true
                    }
                ],
                Localize("descCont", "PerfectShield")
            ),

            _ => (Difficulty, [], "")
        };
        partialRes.Item1 = Math.Max(0, partialRes.Item1);
        return partialRes;
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
            checkAction.itemColor = locationItemColor;
            if (IsLocal() || locationGameName == "Cobalt Core")
            {
                if (Archipelago.ItemToCard.ContainsKey(locationItemName))
                    checkAction.givenCard = locationItemName;
                else if (Archipelago.ItemToArtifact.ContainsKey(locationItemName))
                    checkAction.givenArtifact = locationItemName;
                else if (Archipelago.ItemToModifier.ContainsKey(locationItemName))
                    checkAction.givenModifier = locationItemName;
                else if (Archipelago.ItemToDeck.ContainsKey(locationItemName))
                    checkAction.givenCharacter = locationItemName;
            }
        }
        
        var list = new List<CardAction> { checkAction };
        
        if (GetDraw(s) > 0)
            list.Add(new ADrawCard
            {
                count = GetDraw(s)
            });
        
        list.AddRange(GetUpgradeData(s).actions);
        
        return list;
    }

    private static string Localize(params string[] key) =>
        ModEntry.Instance.Localizations.Localize(new List<string> { "card", "CheckLocationCard" }
                                                     .Concat(key).ToArray());

    internal bool LocationAlreadyChecked()
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        return Archipelago.Instance.APSaveData.LocationsChecked.Contains(locationName);
    }

    public override CardData GetData(State state)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        string? description;
        if (LocationAlreadyChecked())
        {
            // Location was already checked (this is relevant even if we don't scout)
            description = Localize("descNothing");
        }
        else if (locationSlotName is null || locationItemName is null)
        {
            // Location was not scouted
            description = Localize("descNotFound");
        }
        else if (IsLocal())
        {
            // Location was scouted, not already checked, item is local
            description = Localize(WillAddCardToDeck(state) ? "descSelfAddCard"
                                   : WillAddArtifact(state) ? "descSelfAddArtifact"
                                   : WillAddModifier() ? "descSelfAddModifier"
                                   : "descSelf");
            description = string.Format(description, locationItemName);
        }
        else
        {
            // Location was scouted, not already checked, item is from another game
            // Truncate the text to fit in the card
            var varTextLength = locationItemName.Length + locationSlotName.Length;
            var charsToRemove = varTextLength - 28;
            var effItemName = locationItemName;
            if (charsToRemove > 0)
                effItemName = effItemName.Remove(Math.Max(effItemName.Length - charsToRemove, 0)) + "...";

            description = Localize("descBase");
            description = string.Format(description,
                                        $"<c={locationItemColor}>{effItemName}</c>",
                                        $"<c={APColors.OtherPlayer}>{locationSlotName}</c>");
        }

        if (GetDraw(state) > 0)
        {
            description += "\n" + string.Format(Localize("descDraw"), GetDraw(state));
        }

        var upgradeData = GetUpgradeData(state);

        if (upgradeData.desc != "") description += "\n" + upgradeData.desc;
        
        return new CardData
        {
            cost = upgradeData.cost,
            singleUse = true,
            description = description,
            art = this switch
            {
                // Art by item classification
                { locationItemColor: APColors.Progression } => ArtRare,
                { locationItemColor: APColors.Useful } => ArtUncommon,
                { locationItemColor: APColors.Filler or APColors.Trap } => ArtCommon,
                // If locationItemColor is null, do by rarity instead
                CheckLocationCardUncommon => ArtUncommon,
                CheckLocationCardRare => ArtRare,
                _ => ArtCommon
            },
            artTint = "CCCCCC"
        };
    }

    private static int Difficulty => Archipelago.InstanceSlotData.CheckCardDifficulty;

    private int GetShield(State _) => Difficulty switch
        {
            <= 0 => 1,
            <= 1 => 2,
            <= 2 => 3,
            _ => 4
        };

    private bool IsShieldTemp(State _) => Difficulty <= 3;
    
    private int GetAttack(State s) => GetDmg(s, Difficulty switch
    {
        <= 1 => 1,
        2 => 2,
        _ => 3
    });

    private int GetAttackTimes(State _) => Difficulty <= 3 ? 2 : 3;

    private int GetStunAttack(State s) => GetDmg(s, Difficulty switch
    {
        <= 1 => 1,
        2 => 2,
        _ => 3
    });

    private int GetDraw(State _)
    {
        var total = 0;
        if (locationFrom == Deck.riggs && upgrade == Upgrade.A)
            total += Difficulty switch
            {
                <= 2 => 1,
                3 => 2,
                _ => 3
            };
        if (Difficulty < 0) total += 1;
        return total;
    }

    private int GetMove(State _) => Difficulty switch
    {
        <= 1 => 1,
        2 => 2,
        3 => 3,
        _ => 4
    };

    private int GetDroneshift(State _) => Difficulty switch
    {
        <= 2 => 1,
        3 => 2,
        _ => 3
    };

    private int GetShard(State _) => Difficulty switch
    {
        <= 2 => 2,
        _ => 3
    };

    private int GetBoost(State _) => Difficulty switch
    {
        <= 2 => 1,
        3 => 2,
        _ => 3
    };
    
    private bool IsLocal()
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        return locationSlotName == Archipelago.Instance.APSaveData.Slot;
    }

    private bool HasDeck(State state) =>
        locationItemName is not null
        && (
            (Archipelago.ItemToCard.TryGetValue(locationItemName, out var cardType)
             && DB.cardMetas.TryGetValue(cardType.Name, out var cardMeta)
             && state.characters.Any(character => character.deckType == cardMeta.deck))
            || (Archipelago.ItemToArtifact.TryGetValue(locationItemName, out var artifactType)
                && DB.artifactMetas.TryGetValue(artifactType.Name, out var artifactMeta)
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

    private bool WillAddModifier()
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        if (locationItemName is null) return false;
        if (!IsLocal()) return false;
        if (!Archipelago.ItemToModifier.TryGetValue(locationItemName, out var modifier)) return false;
        if (ItemApplier.StartOnlyModifiers.Contains(modifier)) return false;
        if (Archipelago.Instance.APSaveData.HasItem(locationItemName)) return false;
        if (Archipelago.InstanceSlotData.ImmediateRewardsBlacklist.Contains(locationItemName)) return false;
        return Archipelago.InstanceSlotData.ModifiersMode is ModifierShuffleMode.Immediate
            or ModifierShuffleMode.ImmediateAndUnlockable;
    }

    internal void LoadInfo(ScoutedItemInfo? info)
    {
        if (info is null)
        {
            locationItemName = "[]";
            locationSlotName = "[]";
            locationItemColor = APColors.Trap;
        }
        else
        {
            string itemColor = info.GetColor();
            locationItemName = info.ItemName;
            locationSlotName = info.Player.Name;
            locationGameName = info.ItemGame;
            locationItemColor = itemColor;
        }
    }

    public override void AfterWasPlayed(State state, Combat c)
    {
        if (LocationAlreadyChecked()) return;
        APFX(GetScreenRect() + pos + new Vec(Combat.marginRect.x, Combat.marginRect.y));
        Audio.Play(FSPRO.Event.Story_BooksTeleport);
    }

    private static void APFX(
        Rect cardRectScreenPos)
    {
        var center = new Vec(cardRectScreenPos.x + cardRectScreenPos.w / 2.0,
                             cardRectScreenPos.y + cardRectScreenPos.h / 2.0 + 10.0);
        var colors = new List<Color>
        {
            new(0xD9A07D80),
            new(0x767EBD80),
            new(0xEEE39180),
            new(0xC9768280),
            new(0x77C67780),
            new(0xCA94C280)
        };
        for (var index = 0; index < 50; ++index)
        {
            var vel = Mutil.RandVel();
            var startPos = center + vel * 20.0;
            PFX.screenSpaceAdd.Add(new Particle
            {
                pos = startPos,
                size = 1.0 + 4.0 * (3.0 * Mutil.NextRand()),
                vel = vel * 200.0,
                color = colors[Mutil.random.Next(0, 6)],
                dragCoef = 2.0 + 4.0 * Mutil.NextRand(),
                lifetime = 0.7 + 0.6 * Mutil.NextRand(),
                gravity = 0.2
            });
        }
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
