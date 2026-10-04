using System;
using CobaltCoreArchipelago.Cards;
using HarmonyLib;

namespace CobaltCoreArchipelago.Features;

[HarmonyPatch(typeof(AAttack), nameof(AAttack.Begin))]
public class AAttackBeginPatch
{
    static void Postfix(AAttack __instance, Combat c)
    {
        if (!__instance.fromDroneX.HasValue) return;
        c.stuff.TryGetValue(__instance.fromDroneX.Value, out var stuff);
        if (stuff is not Archidrone) return;
        var reverse = __instance.targetPlayer ^ stuff.targetPlayer;
        stuff.pulse = reverse ? -1.0 : 1.0;
    }
}

[HarmonyPatch(typeof(StuffBase), nameof(StuffBase.Update))]
public class StuffBaseUpdatePatch
{
    static void Prefix(StuffBase __instance, out double __state)
    {
        __state = __instance.pulse;
    }

    static void Postfix(StuffBase __instance, double __state, G g)
    {
        __instance.pulse = __state > 0.0
            ? Math.Max(0.0, __state - g.dt * 2.0)
            : Math.Min(0.0, __state + g.dt * 2.0);
    }
}