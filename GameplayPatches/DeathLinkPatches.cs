using System.Diagnostics;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using HarmonyLib;
using Microsoft.Extensions.Logging;

namespace CobaltCoreArchipelago.GameplayPatches;

public class DeathLinkPatches;

[HarmonyPatch(typeof(State), nameof(State.EndRun))]
public static class EndRunPatch
{
    static void Prefix(State __instance)
    {
        if ((!Archipelago.Instance.APSaveData?.DeathLinkActive ?? false) || Archipelago.Instance.PreventDeathLink)
        {
            ModEntry.Instance.Logger.LogInformation("Tried sending a DeathLink, but it was prevented");
            Archipelago.Instance.PreventDeathLink = false;
        }
        else
        {
            Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
            Debug.Assert(Archipelago.Instance.DeathLinkService != null, "Archipelago.Instance.DeathLinkService != null");
            var deathCause = __instance.route is Combat { otherShip.ai: not null } combat
                ? $"was killed by {combat.otherShip.ai.GetLocName()}"
                : "died suddenly";
            var combats = __instance.storyVars.combatsThisRun;
            var loop = __instance.storyVars.runCount; 
            deathCause +=
                $" after {combats} {(combats > 1 ? "fights" : "fight")} in Loop {loop}";
            ModEntry.Instance.Logger.LogInformation("Sending a DeathLink with cause: {deathCause}", deathCause);
            Archipelago.Instance.DeathLinkService.SendDeathLink(
                new DeathLink(Archipelago.Instance.APSaveData.Slot, deathCause)
            );
        }
    }
}