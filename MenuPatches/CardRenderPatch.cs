using System;
using System.Diagnostics;
using HarmonyLib;

namespace CobaltCoreArchipelago.MenuPatches;

[HarmonyPatch(typeof(Card), nameof(Card.Render))]
public static class CardRenderPatch
{
    public static void Postfix(Card __instance, G g, Vec? posOverride, bool isInCombatHand)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        if (!Archipelago.InstanceSlotData.ShuffleCards) return;
        if (Archipelago.Instance.APSaveData.HasCardOrNotAP(__instance.GetType())) return;
        if (isInCombatHand) return;
        var pos = posOverride ?? __instance.pos;
        var rect = (__instance.GetScreenRect()
                    + pos
                    + new Vec(y: __instance.hoverAnim * -2.0
                                 + Mutil.Parabola(__instance.flipAnim) * -10.0
                                 + Mutil.Parabola(Math.Abs(__instance.flopAnim)) * -10.0 * Math.Sign(__instance.flopAnim))).round();
        var b = g.Push(rect: rect);
        Draw.Rect(b.rect.x + 1, b.rect.y + 1, b.rect.w - 3, b.rect.h - (__instance.isForeground ? 2 : 0),
                  color: new Color(0.0, 0.0, 0.0, 0.75));
        g.Pop();
    }
}