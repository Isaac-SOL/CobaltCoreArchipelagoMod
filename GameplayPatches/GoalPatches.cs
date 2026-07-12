using System.Diagnostics;
using HarmonyLib;
using Microsoft.Extensions.Logging;

namespace CobaltCoreArchipelago.GameplayPatches;

// NOTE: Also look at VaultPatches.cs

public class GoalPatches;

[HarmonyPatch(typeof(StoryVars), nameof(StoryVars.OnBeatFinale))]
public class OnBeatFinalePatch
{
    static void Postfix()
    {
        Debug.Assert(Archipelago.Instance.Session != null, "Archipelago.Instance.Session != null");
        ModEntry.Instance.Logger.LogInformation("FinaleFrienemy beaten, set AP goal achieved");
        Archipelago.Instance.Session.SetGoalAchieved();
    }
}