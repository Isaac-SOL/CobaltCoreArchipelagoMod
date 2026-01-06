using System;
using System.Collections.Generic;
using System.Diagnostics;
using CobaltCoreArchipelago.Cards;
using HarmonyLib;
using Nickel;

namespace CobaltCoreArchipelago.Actions;

public class AArchipelagoCheckLocation : CardAction
{
    public static Spr Spr;

    public string? locationName;
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
    
    public override List<Tooltip> GetTooltips(State s)
    {
        List<Tooltip> tooltips =
        [
            new GlossaryTooltip($"AArchipelagoCheckLocation")
            {
                Icon = Spr,
                Title = ModEntry.Instance.Localizations.Localize(["action", "AArchipelagoCheckLocation", "title"]),
                TitleColor = Colors.card,
                Description = ModEntry.Instance.Localizations.Localize(["action", "AArchipelagoCheckLocation", "desc"])
            }
        ];
        
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