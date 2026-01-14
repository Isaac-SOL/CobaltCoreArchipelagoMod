using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CobaltCoreArchipelago.Cards;
using HarmonyLib;
using Nickel;

namespace CobaltCoreArchipelago.Actions;

public class AArchipelagoCheckLocation : CardAction
{
    public static Spr Spr;

    public string? locationName;
    public string? itemName;
    public string? receiverName;
    public string? givenCard;
    public string? givenArtifact;

    public override void Begin(G g, State s, Combat c)
    {
        Debug.Assert(locationName != null, nameof(locationName) + " != null");
        Archipelago.Instance.CheckLocation(locationName);
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
                TitleColor = Colors.card,
                Description = Localize("desc"),
            }
        ];

        if (itemName != null)
            tooltips.Add(new TTText(string.Format(Localize("descItem"), itemName)));
        if (receiverName != null)
            tooltips.Add(new TTText(string.Format(Localize("descReceiver"), receiverName)));
        
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