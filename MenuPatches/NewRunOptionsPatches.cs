using HarmonyLib;

namespace CobaltCoreArchipelago.MenuPatches;

public class NewRunOptionsPatches;



// Remove dailies button, not supported
[HarmonyPatch(typeof(NewRunOptions), nameof(NewRunOptions.AreDailiesUnlocked))]
public class DailiesPatch
{
    public static void Postfix(ref bool __result)
    {
        __result = false;
    }
}