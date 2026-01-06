using System.Diagnostics;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using HarmonyLib;

namespace CobaltCoreArchipelago.GameplayPatches;

public class DeathLinkPatches;

[HarmonyPatch(typeof(State), nameof(State.EndRun))]
public static class EndRunPatch
{
    static void Prefix(State __instance)
    {
        if (Archipelago.Instance.PreventDeathLink)
        {
            Archipelago.Instance.PreventDeathLink = false;
        }
        else
        {
            Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
            Debug.Assert(Archipelago.Instance.DeathLinkService != null, "Archipelago.Instance.DeathLinkService != null");
            var deathCause = __instance.route is Combat combat
                ? $"was killed by {combat.otherShip.GetName()}"
                : "died suddenly";
            var combats = __instance.storyVars.combatsThisRun;
            var loop = __instance.storyVars.runCount + 1; 
            deathCause +=
                $" after {combats} {(combats > 1 ? "fights" : "fight")} in Loop {loop}";
            Archipelago.Instance.DeathLinkService.SendDeathLink(
                new DeathLink(Archipelago.Instance.APSaveData.Slot, deathCause)
            );
        }
    }
}