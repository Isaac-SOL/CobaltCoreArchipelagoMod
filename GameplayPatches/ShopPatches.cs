using System;
using System.Collections.Generic;
using System.Diagnostics;
using CobaltCoreArchipelago.Actions;
using CobaltCoreArchipelago.Features;
using HarmonyLib;

namespace CobaltCoreArchipelago.GameplayPatches;

[HarmonyPatch(typeof(Events), nameof(Events.NewShop))]
public class ShopPatch
{
    public static void Postfix(List<Choice> __result, State s)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");

        if (CardBrowseListPatch.GetPickableAPCardsList(s).Count > 0)
        {
            __result.Insert(__result.Count - 2, new Choice
            {
                label = ModEntry.Instance.Localizations.Localize(["cardBrowse", "eventMissedAPCardName"]),
                key = ".shopUpgradeCard",
                actions =
                [
                    new AAPCardSelect
                    {
                        browseAction = new CardSelectAdd(),
                        browseSource = CardBrowse.Source.Deck,
                        browseData = new CardBrowseAPData
                        {
                            filterMode = CardBrowseAPData.FilterMode.FoundMissingLocations
                        },
                        allowCancel = true
                    }
                ]
            });
        }
        
        var apArtifactsCount = CardBrowseListPatch.GetPickableAPArtifactsList(s).Count;
        if (CardBrowseListPatch.GetPickableAPArtifactsList(s).Count > 0)
        {
            __result.Insert(__result.Count - 2, new Choice
            {
                label = ModEntry.Instance.Localizations.Localize(["cardBrowse", "eventMissedAPArtifactName"]),
                key = ".shopUpgradeCard",
                actions =
                [
                    new AAPArtifactSelect
                    {
                        mode = ArtifactPick.Mode.MissedAP,
                        allowCancel = true
                    }
                ]
            });
        }
    }
}