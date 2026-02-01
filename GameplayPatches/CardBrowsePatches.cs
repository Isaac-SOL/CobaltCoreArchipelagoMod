using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HarmonyLib;

namespace CobaltCoreArchipelago.GameplayPatches;

public class CardBrowsePatches;

[HarmonyPatch(typeof(CardBrowse), nameof(CardBrowse.GetCardList))]
public class CardBrowseListPatch
{
    public static void Postfix(CardBrowse __instance, ref List<Card> __result, G g)
    {
        var modDataHelper = ModEntry.Instance.Helper.ModData;
        if (!modDataHelper.ContainsModData(__instance, "SelectAPUnlockedCard")) return;
        
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        __instance._listCache.Clear();
        __result.Clear();

        __instance._listCache = Archipelago.Instance.APSaveData.FoundCards
            .Select(cardType => (cardType.CreateInstance() as Card)!)
            .Where(card => g.state.characters.Any(c => c.deckType!.Value == card.GetMeta().deck))
            .ToList();
        __result.AddRange(__instance._listCache);
    }
}
