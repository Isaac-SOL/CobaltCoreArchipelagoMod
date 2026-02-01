using System.Collections.Generic;
using CobaltCoreArchipelago.Actions;
using HarmonyLib;

namespace CobaltCoreArchipelago.GameplayPatches;

[HarmonyPatch(typeof(Events), nameof(Events.BootSequence))]
public class BootSequencePatch
{
    public static void Postfix(List<Choice> __result)
    {
        __result.Add(new Choice
        {
            label = "Pick an unlocked card",
            key = ".zone_first",
            actions = [new AAPCardSelect
            {
                browseAction = new CardSelectAdd(),
                browseSource = CardBrowse.Source.Codex
            }]
        });
    }
}