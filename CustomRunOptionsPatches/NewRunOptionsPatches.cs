using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.Logging;

namespace CobaltCoreArchipelago.CustomRunOptionsPatches;

// Prevent the "Custom" button from showing
[HarmonyPatch]
public class NewRunOptionsCustomPatch
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        return AccessTools.GetTypesFromAssembly(
                AccessTools.AllAssemblies()
                    .First(a => (a.GetName().Name ?? a.GetName().FullName) == "CustomRunOptions"))
            .Where(type => type.Name == "NewRunOptionsButton" && type.Namespace == "Shockah.CustomRunOptions")
            .SelectMany(type => type.GetMethods(AccessTools.all))
            .Where(method => method.Name.StartsWith("NewRunOptions_"));
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