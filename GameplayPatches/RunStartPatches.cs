using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;

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

    public static void Postfix(State __instance)
    {
        if (Archipelago.InstanceSlotData.ModifiersMode is ModifierShuffleMode.Off or ModifierShuffleMode.Immediate)
            return;

        foreach (var modifier in PickModifiersForNextRun())
        {
            __instance.SendArtifactToChar((Artifact)modifier.CreateInstance());
        }
    }

    internal static List<Type> PickModifiersForNextRun()
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        var rand = Archipelago.Instance.APSaveData.ModifiersPickRand;
        
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