using System.Diagnostics;
using HarmonyLib;

namespace CobaltCoreArchipelago.GameplayPatches;

// NOTE: Also look at VaultPatches.cs

public class GoalPatches;

[HarmonyPatch(typeof(StoryVars), nameof(StoryVars.OnBeatFinale))]
public class OnBeatFinalePatch
{
    static void Postfix()
    {
        Debug.Assert(Archipelago.Instance.Session != null, "Archipelago.Instance.Session != null");
        Archipelago.Instance.Session.SetGoalAchieved();
    }
}