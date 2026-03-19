using HarmonyLib;
using Microsoft.Extensions.Logging;

namespace CobaltCoreArchipelago.GameplayPatches;

// Do not give achievements
[HarmonyPatch(typeof(Cheevos), nameof(Cheevos.ShouldDoCheevos))]
[HarmonyPriority(Priority.Low)]
public class CheevosPatch
{
    static void Postfix(ref bool __result)
    {
        if (__result) ModEntry.Instance.Logger.LogDebug("Prevented getting an achievement.");
        __result = false;
    }
}