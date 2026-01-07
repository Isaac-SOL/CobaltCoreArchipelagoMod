using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace CobaltCoreArchipelago.MenuPatches;

public class MainMenuPatches;

[HarmonyPatch(typeof(SharedArt), nameof(SharedArt.DrawCore))]
internal class DrawCorePatch
{
    internal static Spr SmolCobaltSpr;
    
    private static readonly List<Color> CrystalColors = [
        new("F74861"),
        new("FFFF5D"),
        new("4F5FE4"),
        new("FF944A"),
        new("E876D7"),
        new("4BF24B")
    ];

    private static bool GoFront(double myTime) => Math.Cos(myTime - 0.2) > 0;

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> storedInstructions = new(instructions);
        var codeMatcher = new CodeMatcher(storedInstructions, generator);
        // Draw mini cobalts after the background sprite, but before the core
        codeMatcher.MatchEndForward(
                CodeMatch.WithOpcodes([OpCodes.Brfalse])
            ).ThrowIfInvalid("Could not find branch in instructions")
            .InsertAfter(
                // Load all arguments from the base function and call PreCobalt with them
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldarg_2),
                CodeInstruction.Call((G g, bool showCobalt, Vec offset) => PreCobalt(g, showCobalt, offset))
            );

        return codeMatcher.Instructions();
    }

    static void PreCobalt(G g, bool showCobalt, Vec offset)
    {
        if (!showCobalt) return;
        DisplayMiniCobalts(g, offset, false);
    }

    static void Postfix(G g, bool showCobalt, Vec offset)
    {
        if (!showCobalt) return;
        DisplayMiniCobalts(g, offset, true);
    }

    static void DisplayMiniCobalts(G g, Vec offset, bool front)
    {
        var cobaltX = offset.x + G.screenSize.x / 2.0;
        var cobaltY = offset.y + G.screenSize.y / 2.0 + Math.Sin(g.time * 2.0 - 1.0) * 4.0;
        for (int i = 0; i < 6; i++)
        {
            var myTime = ((g.time * 0.473) + i * Math.PI) / 3.0;
            if (front != GoFront(myTime)) continue;
            var circleOffsetX = Math.Sin(myTime) * 64.0;
            var circleOffsetY = Math.Cos(myTime) * 32.0;
            circleOffsetX -= circleOffsetY / 1.7;
            Draw.Sprite(SmolCobaltSpr,
                        cobaltX + circleOffsetX,
                        cobaltY + circleOffsetY,
                        originRel: new Vec(0.5, 0.5),
                        color: CrystalColors[i]
            );
        }
    }

    public static void DrawCore(G g, bool showCobalt, Vec offset)
    {
        Draw.Sprite(StableSpr.cockpit_deletionChamber, offset.x, offset.y);
        if (showCobalt)
        {
            var cobaltX = offset.x + G.screenSize.x / 2.0;
            var cobaltY = offset.y + G.screenSize.y / 2.0 + Math.Sin(g.time * 2.0) * 4.0;
            Draw.Sprite(StableSpr.cockpit_cobalt_core, cobaltX, cobaltY, originRel: new Vec(0.5, 0.5));
        }
        Voronois.DustMotes(g, 40.0, Colors.defaultDust, g.time, G.screenSize / 2.0, G.screenSize);
    }
}

[HarmonyPatch(typeof(MainMenu), nameof(MainMenu.Render))]
public class MainMenuRenderPatch
{
    internal static Spr ArchipelagoTitleSpr;
    
    static void Postfix(MainMenu __instance)
    {
        if (__instance.subRoute is not null) return;
        Draw.Sprite(ArchipelagoTitleSpr, 19.0, 90.0);
    }
}