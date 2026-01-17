using HarmonyLib;

namespace CobaltCoreArchipelago.MenuPatches;

public class NewRunOptionsPatches;

// No, dailies are never unlocked because we don't support them
[HarmonyPatch(typeof(NewRunOptions), nameof(NewRunOptions.AreDailiesUnlocked))]
public class DailiesPatch
{
    public static void Postfix(ref bool __result)
    {
        __result = false;
    }
}

// Actually just prevent dailies button from being drawn at all
[HarmonyPriority(Priority.High)]
[HarmonyPatch(typeof(SharedArt), nameof(SharedArt.ButtonText))]
public class ButtonTextPatch
{
    public static bool Prefix(UIKey key)
    {
        return key.k != StableUK.newRun_dailyRun;
    }
}

// Unhide Books and CAT
[HarmonyPatch(typeof(Character), nameof(Character.Render))]
public class CharacterRenderPatch
{
    public static void Prefix(ref bool hideFace)
    {
        hideFace = false;
    }
}