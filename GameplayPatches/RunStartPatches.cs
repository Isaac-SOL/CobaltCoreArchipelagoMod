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
        
        foreach (var modifier in PickModifiersForNextRun())
            state.SendArtifactToChar((Artifact)modifier.CreateInstance());
    }

    internal static List<Type> PickModifiersForNextRun()
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        var rand = Archipelago.Instance.APSaveData.ModifiersPickRand;
        
        ModEntry.Instance.Logger.LogInformation("Picking modifiers with seed: {modSeed}", rand.seed);
        
        var modifiersAmount = 1 + rand.NextInt() % 3;
        List<Type> picked = [];
        foreach (var modifier in Archipelago.Instance.APSaveData.FoundModifiers
                     .ExceptBy(Archipelago.Instance.APSaveData.LastRunModifiers, m => m.Name)
                     .Shuffle(rand)
                     .Take(modifiersAmount))
        {
            if (!picked.Any(alreadyPicked => DailyDescriptor.AreDailyModifierArtifactsMutuallyExclusive(modifier.Name, alreadyPicked.Name)))
            {
                picked.Add(modifier);
                ModEntry.Instance.Logger.LogInformation("Adding modifier: {modifier}", modifier);
            }
        }
        if (picked.Contains(typeof(DailyDraftPick)) && picked.Contains(typeof(DailyBossArtifactTreat)))
        {
            picked.Remove(typeof(DailyDraftPick));
            picked.Add(typeof(DailyDraftPick));
        }

        Archipelago.Instance.APSaveData.LastRunModifiers = picked.Select(m => m.Name).ToHashSet();
        APSaveData.Save();
        
        return picked;
    }
}