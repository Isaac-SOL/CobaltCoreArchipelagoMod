using HarmonyLib;

namespace CobaltCoreArchipelago.ConnectionInfoMenu;

[HarmonyPatch(typeof(State), nameof(State.LoadOrNew))]
public class FileLoadPatch
{
    static void Prefix()
    {
        ModEntry.Instance.Archipelago.Reconnect("localhost", 38281, "Time Crystal");
    }
}

[HarmonyPatch(typeof(State), nameof(State.NewGame))]
public class NewGamePatch
{
    // ReSharper disable once RedundantAssignment
    static void Prefix(int? slot, uint? seed, MapBase? map, ref bool skipTutorial)
    {
        skipTutorial = true;
    }
}