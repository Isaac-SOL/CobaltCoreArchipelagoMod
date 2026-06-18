using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.Logging;

namespace CobaltCoreArchipelago.CustomRunOptionsPatches;

// Prevent the "Custom" button from showing
public class NewRunOptionsCustomPatch
{
    public static void MaybeApply(Harmony harmony)
    {
        if (ModEntry.Instance.CROAssembly is not { } cro) return;
        foreach (var method in AccessTools.GetTypesFromAssembly(cro)
                     .Where(type => type.Name == "NewRunOptionsButton" && type.Namespace == "Shockah.CustomRunOptions")
                     .SelectMany(type => type.GetMethods(AccessTools.all))
                     .Where(method => method.Name.StartsWith("NewRunOptions_")))
        {
            harmony.Patch(
                original: method,
                prefix: typeof(NewRunOptionsCustomPatch).GetMethod(nameof(Prefix))
            );
        }
    }

    public static bool Prefix() => false;
}

// Unmanned runs are essentially CAT runs wrt AP, so we don't allow them
[HarmonyPatch(typeof(RunConfig), nameof(RunConfig.IsValid))]
[HarmonyPriority(Priority.VeryLow)]
public class RunConfigUnmannedInvalidPatch
{
    public static void Postfix(RunConfig __instance, ref bool __result)
    {
        if (__instance.selectedChars.Count == 0)
            __result = false;
    }
}