using HarmonyLib;

namespace CobaltCoreArchipelago.GameplayPatches;

public class MainLoopPatches;

// Process received Archipelago items
[HarmonyPatch(typeof(G), nameof(G.OnAfterFrame))]
public static class AfterFramePatch
{
    public static void Prefix(G __instance)
    {
        if (ItemApplier.CanApplyItems)
            Archipelago.Instance.SafeUpdate(__instance.state);
    }
}