using System;
using System.Collections.Generic;
using CobaltCoreArchipelago.Actions;
using Nickel;

namespace CobaltCoreArchipelago.Cards;

public class Archidrone : StuffBase
{
    internal static Spr Sprite;
    internal static Spr Icon;

    public override bool IsHostile() => targetPlayer;

    public override void Render(G g, Vec v)
    {
        DrawWithHilight(g, Sprite, v + GetOffset(g) + new Vec(1.0, 10.0), flipY: targetPlayer);
    }

    public override Vec GetOffset(G g, bool doRound = false)
    {
        // Copied from the decompiled IL, to ease the clamp on pulse
        var vec1 = new Vec();
        var flag = targetPlayer == fromPlayer;
        var vec2 = new Vec(y: (1.0 - yAnimation) * (targetPlayer ? -16.0 : 16.0) * (yAnimation < 1.0 & flag ? -1.0 : 1.0));
        var vec3 = vec1 + vec2 + new Vec(y: (targetPlayer ? -16.0 : 16.0) * Mutil.Clamp(pulse * 1.3, -1.0, 1.0));
        var wiggleRate = GetWiggleRate();
        var num = Mutil.Rand(x) * Math.PI * 2.0;
        var vec4 = new Vec(Math.Sin(num + g.state.time * wiggleRate), Math.Cos(num + Math.PI + g.state.time * wiggleRate * 1.3));
        if (doRound)
            vec4 = vec4.round();
        var vec5 = GetWiggleAmount() * vec4;
        return vec3 + vec5;
    }

    public override List<Tooltip> GetTooltips()
    {
        var tooltips = new List<Tooltip>
        {
            new GlossaryTooltip("AArchiprismTooltip")
            {
                Icon = Icon,
                Title = ModEntry.Instance.Localizations.Localize(["card", "Archidrone", "droneName"]),
                TitleColor = Colors.midrow,
                Description = ModEntry.Instance.Localizations.Localize(["card", "Archidrone", "droneDesc"]),
                FlipIconY = targetPlayer
            }
        };
        if (bubbleShield)
            tooltips.Add(new TTGlossary("midrow.bubbleShield"));
        return tooltips;
    }

    public override Spr? GetIcon() => Icon;

    public override string GetDialogueTag() => "attackDrone";

    public override List<string> PossibleDroneNames() => AttackDrone.droneNames;
    
}