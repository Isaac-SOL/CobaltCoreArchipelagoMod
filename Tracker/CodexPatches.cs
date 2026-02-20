using HarmonyLib;

namespace CobaltCoreArchipelago;

public class CodexPatches;

[HarmonyPatch(typeof(Codex), nameof(Codex.Render))]
public static class CodexRenderPatch
{
    public static void Postfix(Codex __instance, G g)
    {
        if (__instance.subRoute is not null) return;
        
        SharedArt.MenuItem(
            g,
            new Vec(140.0, 226.0),
            180,
            false,
            ArchipelagoUK.codex_apTracker.ToUK(),
            ModEntry.Instance.Localizations.Localize(["codex", "trackerName"]),
            __instance
        );
    }
}

[HarmonyPatch(typeof(Codex), nameof(Codex.OnMouseDown))]
public static class CodexMouseDownPatch
{
    public static void Postfix(Codex __instance, Box b)
    {
        if (b.key == ArchipelagoUK.codex_apTracker.ToUK())
        {
            Audio.Play(FSPRO.Event.Click);
            __instance._lastSelected = Input.currentGpKey;
            __instance.subRoute = new Tracker();
        }
    }
}