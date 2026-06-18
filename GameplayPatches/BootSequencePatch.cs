using System;
using System.Collections.Generic;
using System.Diagnostics;
using CobaltCoreArchipelago.Actions;
using CobaltCoreArchipelago.Features;
using HarmonyLib;

namespace CobaltCoreArchipelago.GameplayPatches;

[HarmonyPatch(typeof(Events), nameof(Events.BootSequence))]
public class BootSequencePatch
{
    public static void Postfix(List<Choice> __result, State s)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        
        if (CardBrowseListPatch.GetPickableUnlockedCardsList(s).Count
            + CardBrowseListPatch.GetPickableUnlockedArtifactsList(s).Count > 0)
        {
            __result.Add(new Choice
            {
                label = ModEntry.Instance.Localizations.Localize(["cardBrowse", "bootOptionUnlockedItemName"]),
                key = ".saltyisaac_archipelago_bootSequenceUnlockedItem"
            });
        }
    }

    public static List<Choice> BootSequencePickUnlockedItem(State s)
    {
        
        List<Choice> choices = [];

        if (CardBrowseListPatch.GetPickableUnlockedCardsList(s).Count > 0)
        {
            choices.Add(new Choice
            {
                label = ModEntry.Instance.Localizations.Localize(["cardBrowse", "bootOptionUnlockedCardName"]),
                key = ".zone_first",
                actions =
                [
                    new AAPCardSelect
                    {
                        browseAction = new CardSelectAdd(),
                        browseSource = CardBrowse.Source.Deck,
                        browseData = new CardBrowseAPData
                        {
                            filterMode = CardBrowseAPData.FilterMode.UnlockedCardsNotInDeck
                        },
                        allowCancel = true
                    }
                ]
            });
        }

        var pickableArtifactsCount = CardBrowseListPatch.GetPickableUnlockedArtifactsList(s).Count;
        if (pickableArtifactsCount > 0)
        {
            choices.Add(new Choice
            {
                label = ModEntry.Instance.Localizations.Localize(["cardBrowse", "bootOptionUnlockedArtifactName"]),
                key = ".zone_first",
                actions =
                [
                    new AAPArtifactSelect
                    {
                        mode = ArtifactPick.Mode.Unlocked,
                        allowCancel = true
                    }
                ]
            });
        }
        
        choices.Add(new Choice
        {
            label = Loc.T("ShopSkipConfirm_No", "On second thought..."),
            key = "BootSequence"
        });

        return choices;
    }
    
}