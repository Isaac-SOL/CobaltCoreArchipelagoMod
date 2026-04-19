using CobaltCoreArchipelago.Actions;
using HarmonyLib;

namespace CobaltCoreArchipelago.MenuPatches;

[HarmonyPatch(typeof(Dialogue), nameof(Dialogue.DrawChoice))]
public class DialogueChoicePatch
{
    public static void Prefix(G g, Choice opt, double yOffset)
    {
        if (yOffset <= 50) return;
        var textRect = Draw.Text("> " + opt.label, 0.0, 0.0, maxWidth: 158.0, dontDraw: true);
        var choiceHeight = textRect.h + 6.0;
        var textBox = g.Push(rect: new Rect(-5.0, yOffset, 170.0, choiceHeight));
        var dialogueBoxHeight = choiceHeight + 5.0;
        Draw.Sprite(
            StableSpr.panels_overworld_dialogue,
            textBox.rect.x - 3.0, textBox.rect.y,
            pixelRect: new Rect(0, 90 - dialogueBoxHeight, 176, dialogueBoxHeight)
        );
        g.Pop();
    }
}