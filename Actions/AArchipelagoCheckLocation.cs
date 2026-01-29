using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CobaltCoreArchipelago.Cards;
using HarmonyLib;
using Nickel;
using TheJazMaster.CombatQoL;

namespace CobaltCoreArchipelago.Actions;

public class AArchipelagoCheckLocation : CardAction
{
    public static Spr Spr;

    public string? locationName;
    public string? itemName;
    public string? itemColor;
    public string? receiverName;
    public string? givenCard;
    public string? givenArtifact;

    public override void Begin(G g, State s, Combat c)
    {
        Debug.Assert(locationName != null, nameof(locationName) + " != null");
        // Can't undo an AP card with CombatQoL
        var combatQoL = ModEntry.Instance.CombatQol;
        combatQoL?.InvalidateUndos(c, ICombatQolApi.InvalidationReason.CUSTOM_REASON, "sending an Archipelago item");
        if (!combatQoL?.IsSimulating() ?? true) Archipelago.Instance.CheckLocation(locationName);
    }
    
    public override Icon? GetIcon(State s)
    {
        return new Icon
        {
            path = Spr
        };
    }

    private static string Localize(params string[] key) =>
        ModEntry.Instance.Localizations.Localize(new List<string> { "action", "AArchipelagoCheckLocation" }
                                                     .Concat(key).ToArray());
    
    public override List<Tooltip> GetTooltips(State s)
    {
        List<Tooltip> tooltips =
        [
            new GlossaryTooltip($"AArchipelagoCheckLocation")
            {
                Icon = Spr,
                Title = Localize("title"),
                TitleColor = Colors.boldPink,
                Description = Localize("desc"),
            }
        ];

        if (itemName != null)
            tooltips.Add(new TTText(string.Format(Localize("descItem"), $"<c={itemColor}>{itemName}</c>")));
        if (receiverName != null)
        {
            Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
            var receiverColor = receiverName == Archipelago.Instance.APSaveData.Slot
                ? APColors.Self
                : APColors.OtherPlayer;
            tooltips.Add(new TTText(string.Format(Localize("descReceiver"), $"<c={receiverColor}>{receiverName}</c>")));
        }
        
        if (givenCard != null)
        {
            tooltips.Add(new TTCard
            {
                card = (Card) Archipelago.ItemToCard[givenCard].CreateInstance()
            });
        }

        if (givenArtifact != null)
        {
            var artifact = (Artifact) Archipelago.ItemToArtifact[givenArtifact].CreateInstance();
            tooltips.Add(new TTDivider());
            tooltips.AddRange(artifact.GetTooltips());
            tooltips.Add(new TTDivider());
        }

        return tooltips;
    }
}