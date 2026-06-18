using System;
using System.Collections.Generic;
using System.Diagnostics;
using CobaltCoreArchipelago.Cards;
using HarmonyLib;

namespace CobaltCoreArchipelago.MenuPatches;

[HarmonyPatch(typeof(Card), nameof(Card.Render))]
public static class CardRenderPatch
{
    internal static Dictionary<Deck, Spr> FrameOverlays = [];
    
    public static void Postfix(Card __instance, G g, Vec? posOverride, bool ignoreAnim, bool hideFace)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        
        var pos = posOverride ?? __instance.pos;
        var rect = (__instance.GetScreenRect()
                    + pos
                    + new Vec(y: __instance.hoverAnim * -2.0
                                 + Mutil.Parabola(__instance.flipAnim) * -10.0
                                 + Mutil.Parabola(Math.Abs(__instance.flopAnim)) * -10.0 * Math.Sign(__instance.flopAnim))).round();

        if (__instance is CheckLocationCard { locationFrom: not null } apCard
            && !hideFace
            && (ignoreAnim
                || ((apCard.drawAnim != 0.0 || apCard.waitBeforeMoving <= 0.0)
                    && !(apCard.drawAnim < 1.0)
                    && !(apCard.flipAnim > 0.0))))
        {
            var b = g.Push(rect: rect);
            Draw.Sprite(FrameOverlays[apCard.locationFrom.Value], b.rect.x, b.rect.y);
            g.Pop();
        }
        
        if (!Archipelago.InstanceSlotData.ShuffleCards) return;
        
        if (Archipelago.Instance.APSaveData.HasCardOrNotAP(__instance.GetType())) return;
        // Don't darken in tooltips
        if (ModEntry.Instance.Helper.ModData.TryGetModData(__instance, "tooltipCard", out bool tt) && tt) return;
        // All cards are unlocked during the finale (for now at least)
        if (g.state.route is Combat { otherShip.ai: FinaleFrienemy }) return;
        var b2 = g.Push(rect: rect);
        Draw.Rect(b2.rect.x + 1, b2.rect.y + 1,
                  b2.rect.w - 3 + (Input.gamepadIsActiveInput ? 1 : 0), b2.rect.h - (__instance.isForeground ? 2 : 0),
                  color: new Color(0.0, 0.0, 0.0, 0.75));
        g.Pop();
    }
}

[HarmonyPatch(typeof(TTCard), nameof(TTCard.Render))]
public static class TTCardRenderPatch
{
    public static List<Type> cardsInTooltip = [];
    
    [HarmonyPriority(Priority.High)]
    public static void Prefix(TTCard __instance)
    {
        ModEntry.Instance.Helper.ModData.SetModData(__instance.card, "tooltipCard", true);
        cardsInTooltip.Add(__instance.card.GetType());
    }
    
    [HarmonyPriority(Priority.Low)]
    public static void Postfix(TTCard __instance)
    {
        ModEntry.Instance.Helper.ModData.RemoveModData(__instance.card, "tooltipCard");
    }
}
