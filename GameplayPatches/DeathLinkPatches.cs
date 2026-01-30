using System.Diagnostics;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using CobaltCoreArchipelago.Features;
using HarmonyLib;
using Microsoft.Extensions.Logging;

namespace CobaltCoreArchipelago.GameplayPatches;

public class DeathLinkPatches;

[HarmonyPatch(typeof(Combat), nameof(Combat.CheckDeath))]
public static class CheckDeathPatch
{
    internal static bool ThisDeathHandled = false;
    
    static void Prefix(G g)
    {
        var state = g.state;
        // This function is called every frame in combat. We want to send the DeathLink on the first frame only.
        // ThisDeathHandled ensures that ApplyDeathLinkIfNotPrevented is called only once. It will be reset later by EndRunPatch.
        if (state.ship is not { hull: <= 0, deathProgress: not null } || ThisDeathHandled) return;
        ThisDeathHandled = true;
        ApplyDeathLinkIfNotPrevented(state);
    }

    internal static void ApplyDeathLinkIfNotPrevented(State state)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        // We call this function exactly once per death, including deaths caused by received DeathLinks.
        if (Archipelago.Instance.APSaveData.LastCombatCount > state.storyVars.combatsThisRun)
            Archipelago.Instance.APSaveData.LastCombatCount = 0;
        // If PreventDeathLink was set earlier (in the case of a received DeathLink), we consume it and do nothing.
        if ((Archipelago.Instance.APSaveData?.DeathLinkMode ?? DeathLinkMode.Off) == DeathLinkMode.Off || DeathLinkManager.PreventDeathLink)
        {
            ModEntry.Instance.Logger.LogInformation("Tried sending a DeathLink, but it was prevented");
            DeathLinkManager.PreventDeathLink = false;
        }
        else
        {
            Debug.Assert(Archipelago.Instance.DeathLinkService != null, "Archipelago.Instance.DeathLinkService != null");
            var deathCause = state.route is Combat { otherShip.ai: not null } combat
                ? $"was killed by {combat.otherShip.ai.GetLocName()}"
                : "died suddenly";
            var combats = state.storyVars.combatsThisRun - Archipelago.Instance.APSaveData!.LastCombatCount;
            var loop = state.storyVars.runCount; 
            deathCause +=
                $" after {combats} {(combats > 1 ? "fights" : "fight")} in Loop {loop}";
            ModEntry.Instance.Logger.LogInformation("Sending a DeathLink with cause: {deathCause}", deathCause);
            Archipelago.Instance.DeathLinkService.SendDeathLink(
                new DeathLink(Archipelago.Instance.APSaveData!.Slot, deathCause)
            );
            Archipelago.Instance.APSaveData!.DeathLinkCount++;
        }
        Archipelago.Instance.APSaveData!.LastCombatCount = state.storyVars.combatsThisRun;
        APSaveData.Save();
    }
}

[HarmonyPatch(typeof(State), nameof(State.EndRun))]
public static class EndRunPatch
{
    static void Prefix(State __instance)
    {
        // This function is called once per death, at the moment the screen changes.
        // If we are out of combat, it is called instantly when the ship hull hits zero.
        if (CheckDeathPatch.ThisDeathHandled)
        {
            // If we are in combat, ApplyDeathLinkIfNotPrevented was already handled in CheckDeathPatch, so we skip
            CheckDeathPatch.ThisDeathHandled = false;
        }
        else
        {
            // Otherwise we call it ourselves
            CheckDeathPatch.ThisDeathHandled = false;
            CheckDeathPatch.ApplyDeathLinkIfNotPrevented(__instance);
        }
    }
}

[HarmonyPatch(typeof(Narrative), nameof(Narrative.GetVoidShout))]
public static class GetVoidShoutPatch
{
    internal static string? DeathLinkMessage;
    
    static void Postfix(ref string __result)
    {
        if (DeathLinkMessage is null) return;
        // If this was a DeathLink death, replace the end message with our own
        __result = DeathLinkMessage;
        DeathLinkMessage = null;
    }
}
