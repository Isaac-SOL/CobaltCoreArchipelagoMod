using System.Collections.Generic;
using System.Diagnostics;
using HarmonyLib;
using Microsoft.Extensions.Logging;

namespace CobaltCoreArchipelago.MenuPatches;

[HarmonyPatch(typeof(TTGlossary), nameof(TTGlossary.BuildString))]
public class CharTooltipPatch
{
    public static void Postfix(ref string __result, string key)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        if (!key.StartsWith("char")) return;
        foreach (var deck in Archipelago.DeckToItem.Keys)
        {
            if (!key.Contains(deck.Key())) continue;
            var state = MG.inst.g.state;
            __result += "\n" + string.Format(ModEntry.Instance.Localizations.Localize(["miscTooltips", "charMemories"]),
                                             Archipelago.Instance.APSaveData.GetFixTimelineAmount(deck, state));
        }
    }
}