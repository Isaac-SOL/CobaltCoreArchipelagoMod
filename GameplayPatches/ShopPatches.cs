using System.Collections.Generic;
using System.Diagnostics;
using CobaltCoreArchipelago.Actions;
using HarmonyLib;

namespace CobaltCoreArchipelago.GameplayPatches;

[HarmonyPatch(typeof(Events), nameof(Events.NewShop))]
public class ShopPatch
{
    public static void Postfix(List<Choice> __result, State s)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");

        if (CardBrowseListPatch.GetPickableUnlockedCardsList(s).Count <= 0) return;
        
        __result.Insert(__result.Count - 2, new Choice
        {
            label = ModEntry.Instance.Localizations.Localize(["cardBrowse", "eventMissedAPCardName"]),
            actions = [new AAPCardSelect
            {
                browseAction = new CardSelectAdd(),
                browseSource = CardBrowse.Source.Deck,
                browseData = new CardBrowseAPData
                {
                    filterMode = CardBrowseAPData.FilterMode.FoundMissingLocations
                }
            }]
        });
    }
}