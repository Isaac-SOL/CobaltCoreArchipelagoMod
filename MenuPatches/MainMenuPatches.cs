using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;

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

    private static bool GoFront(double myTime) => Math.Cos(myTime - 0.22) > 0;

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
    private const int maxMessages = 5;
    
    internal static Spr ArchipelagoTitleSpr;
    
    static void Postfix(MainMenu __instance)
    {
        if (__instance.subRoute is not null) return;
        // Logo
        Draw.Sprite(ArchipelagoTitleSpr, 19.0, 90.0);
        
        if (Archipelago.Instance.APSaveData is null || !Archipelago.Instance.APSaveData.MessagesInMenu) return;
        
        // Draw AP messages
        // var lastMessages = Archipelago.Instance.MessagesReceived.TakeLast(5).Reverse();
        // var x = 185.0;
        // var y = 270.0;
        // foreach (var message in lastMessages)
        // {
        //     var fakeRect = Draw.Text(message, x, y, maxWidth: 250.0, dontDraw: true);
        //     y -= fakeRect.h + 5.0;
        //     Draw.Text("<c=boldPink>></c>", x - 5.0, y);
        //     Draw.Text(message, x, y, maxWidth: 250.0);
        // }
        
        // Draw AP messages
        // I think in order to do a fade we have no choice but to fade each part separately
        // This is a lot of work each frame though...
        var lastMessages = Archipelago.Instance.MessagePartsReceived.TakeLast(maxMessages).Reverse();
        var x = 185.0;
        var y = 270.0;
        int i = 0;
        foreach (var messageParts in lastMessages)
        {
            var alpha = (double)(maxMessages - i) / maxMessages;
            alpha = Math.Pow(alpha, 0.75);
            var fadedParts = messageParts.Select(part => (part.message, part.color.fadeAlpha(alpha))).ToArray();
            var sb = new StringBuilder();
            foreach (var part in fadedParts)
            {
                sb.Append($"<c={part.Item2}>{part.message}</c>");
            }
            var message = sb.ToString();
            var fakeRect = Draw.Text(message, x, y, maxWidth: 250.0, dontDraw: true);
            y -= fakeRect.h + 5.0;
            var pinkColor = Colors.boldPink.fadeAlpha(alpha);
            Draw.Text($"<c={pinkColor}>></c>", x - 5.0, y, outline: Colors.black);
            Draw.Text(message, x, y, maxWidth: 250.0, outline: Colors.black);
            i++;
        }
    }
}