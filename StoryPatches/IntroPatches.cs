using HarmonyLib;

namespace CobaltCoreArchipelago.StoryPatches;

public class IntroPatches;

[HarmonyPatch(typeof(State), nameof(State.MakeZoneIntroDialogue))]
public class ZoneIntroDialoguePatch
{
    static bool Prefix(State __instance, ref Route __result)
    {
        // Remove intro dialogue, only give bonus options
        __result = Dialogue.MakeDialogueRouteOrSkip(__instance, null, OnDone.visitCurrent);
        return false;
    }
}