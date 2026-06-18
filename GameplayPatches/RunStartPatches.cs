using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.Logging;

namespace CobaltCoreArchipelago.GameplayPatches;

[HarmonyPatch]
public class RunStartPatches
{
    public static MethodBase TargetMethod()
    {
        return typeof(State)
            .GetNestedTypes(AccessTools.all)
            .SelectMany(type => type.GetMethods(AccessTools.all))
            .First(method => method.Name.StartsWith($"<{nameof(State.PopulateRun)}>")
                             && method.ReturnType == typeof(Route));
    }

    public static void Postfix(object __instance)
    {
        if (Archipelago.InstanceSlotData.ModifiersMode is ModifierShuffleMode.Off or ModifierShuffleMode.Immediate)
            return;

        var state = __instance.GetType()
            .GetFields(AccessTools.all)
            .FirstOrDefault(f => f.Name.Contains("this"))
            ?.GetValue(__instance) as State;

        if (state is null) return;

        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        foreach (var modifier in Archipelago.Instance.APSaveData.NextModifierRando)
            state.SendArtifactToChar((Artifact)DB.artifacts[modifier].CreateInstance());
    }
}