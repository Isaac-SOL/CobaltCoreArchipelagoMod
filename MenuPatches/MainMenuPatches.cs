using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using CobaltCoreArchipelago.ConnectionInfoMenu;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CobaltCoreArchipelago.MenuPatches;

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

[HarmonyPatch(typeof(MainMenu))]
public class MainMenuPatch
{
    internal static Spr ArchipelagoTitleSpr, TextBoxSpr, TextBoxHoverSpr;
    internal static string commandText = "";
    internal static bool commandLineSelected = false;
    internal static double blinkStartTime;
    internal static int messagesPos = 0;
    
    internal static int MaxMessages => commandLineSelected ? 20 : 5;
    
    [HarmonyPatch(nameof(MainMenu.Render))]
    [HarmonyPostfix]
    static void RenderPostfix(G g, MainMenu __instance)
    {
        if (__instance.subRoute is not null) return;
        // Logo
        Draw.Sprite(ArchipelagoTitleSpr, 19.0, 90.0);
        
        if (Archipelago.Instance.APSaveData is null || !Archipelago.Instance.APSaveData.MessagesInMenu) return;

        // Guard just in case
        if (g.metaRoute != __instance)
        {
            commandLineSelected = false;
            return;
        }
        
        // Clickable field with background
        var immButton = SharedArt.ButtonSprite(
            g,
            new Rect(x: 175.0, y: 248.0, w: 260.0, h: 21.0),
            new UIKey(ArchipelagoUK.mainMenu_commandLine.ToUK()),
            TextBoxSpr, TextBoxHoverSpr,
            boxColor: Colors.buttonBoxNormal,
            onMouseDown: __instance
        );

        var textToDraw = commandText;
        // Add blinking cursor if selected
        if (commandLineSelected)
        {
            if (blinkStartTime == 0.0) blinkStartTime = g.time;
            var offsetTime = g.time - blinkStartTime;
            if (offsetTime - Math.Floor(offsetTime) < 0.5) textToDraw += "<c=boldPink>|</c>";
        }
        // Draw editable text
        Draw.Text(textToDraw, immButton.v.x + 5.0, immButton.v.y + 8.0,
                  color: immButton.isHover ? Colors.textChoiceHoverActive : Colors.textChoice);
        
        // Draw AP messages
        // I think in order to do a fade we have no choice but to fade each part separately
        // This is a lot of work each frame though...
        lock (Archipelago.messagesReceivedLock)
        {
            var parts = Archipelago.Instance.MessagePartsReceived;
            var partsStart = Math.Max(parts.Count - messagesPos - MaxMessages, 0);
            var partsEnd = Math.Max(parts.Count - messagesPos, 0);
            var lastMessages = parts.Take(new Range(partsStart, partsEnd)).Reverse();
            var x = 185.0;
            var y = 250.0;
            int i = 0;
            foreach (var messageParts in lastMessages)
            {
                var alpha = (double)(MaxMessages - i) / MaxMessages;
                alpha = Math.Pow(alpha, 0.65);
                var fadedParts = messageParts.Select(part => (part.message, part.color.fadeAlpha(alpha))).ToArray();
                var sb = new StringBuilder();
                foreach (var part in fadedParts)
                {
                    var effMessage = string.IsNullOrWhiteSpace(part.message) ? "." : part.message;
                    sb.Append($"<c={part.Item2}>{effMessage}</c>");
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
    
    [HarmonyPatch(nameof(MainMenu.OnMouseDown))]
    [HarmonyPostfix]
    public static void OnMouseDownPostfix(G g, Box b)
    {
        if (b.key == ArchipelagoUK.mainMenu_commandLine.ToUK())
        {
            commandLineSelected = true;
            blinkStartTime = g.time;
        }
        else
        {
            commandLineSelected = false;
        }
    }
    
    [HarmonyPatch(nameof(MainMenu.OnInputPhase))]
    [HarmonyPrefix]
    public static bool OnInputPhasePrefix(MainMenu __instance)
    {
        // If command line is selected we override all standard input on MainMenu
        return !commandLineSelected;
    }
    
    [HarmonyPatch(nameof(MainMenu.OnInputPhase))]
    [HarmonyPostfix]
    public static void OnInputPhasePostfix(MainMenu __instance)
    {
        if (Input.scrollY != 0)
        {
            var dir = Math.Sign(Input.scrollY);
            messagesPos = Math.Clamp(messagesPos + dir, 0, Math.Max(Archipelago.Instance.MessagePartsReceived.Count - 1, 0));
        }
        
        if (!commandLineSelected) return;

        if (Input.GetKeyDown(Keys.Escape))
        {
            commandLineSelected = false;
        }
        else if (Input.GetKeyDown(Keys.Enter) && commandText.Length > 0)
        {
            Debug.Assert(Archipelago.Instance.Session != null, "Archipelago.Instance.Session != null");
            Archipelago.Instance.Session.Say(commandText);
            messagesPos = 0;
            commandText = "";
            blinkStartTime = 0.0;
        }
    }

    internal static void OnTextInput(object? sender, TextInputEventArgs args)
    {
        ModEntry.Instance.Logger.LogDebug("Key: {key}, Character: {character}, Code: {code}",
                                          args.Key, args.Character, (int)args.Character);
        if (!commandLineSelected
            || MG.inst.g.metaRoute is null
            || MG.inst.g.metaRoute.subRoute is not null) return;

        if (char.IsLetterOrDigit(args.Character)
            || char.IsSymbol(args.Character)
            || char.IsPunctuation(args.Character)
            || args.Character == ' ')
        {
            commandText += args.Character;
            blinkStartTime = 0.0; // Ask render loop to reset it
        }
        else if (args.Character == ControlChars.Back)
        {
            if (commandText.Length > 0) commandText = commandText.Remove(commandText.Length - 1);
            blinkStartTime = 0.0; // Ask render loop to reset it
        }
    }
}
