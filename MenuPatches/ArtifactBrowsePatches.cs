using System.Diagnostics;
using HarmonyLib;

namespace CobaltCoreArchipelago.MenuPatches;

[HarmonyPatch(typeof(Artifact), nameof(Artifact.Render))]
public class ArtifactRenderPatch
{
    internal static Spr LockedSpr;
    
    public static void Postfix(Artifact __instance, Vec restingPosition)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        if (Archipelago.InstanceSlotData.ShuffleArtifacts == ArtifactShuffleMode.Off) return;
        if (Archipelago.Instance.APSaveData.HasArtifactOrNotAP(__instance.GetType())) return;
        Draw.Sprite(LockedSpr, __instance.lastScreenPos.x - 2, __instance.lastScreenPos.y - 2);
    }
}