using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CobaltCoreArchipelago.Artifacts;
using CobaltCoreArchipelago.MenuPatches;
using daisyowl.text;
using HarmonyLib;
using Microsoft.Xna.Framework.Input;

namespace CobaltCoreArchipelago.Features;

public class ArtifactPick : Route, OnInputPhase, OnMouseDown
{
    public enum Mode
    {
        Unlocked,
        MissedAP
    }

    public enum BackMode
    {
        None,
        Cancel
    }

    public required List<Artifact> artifactsAvailable;

    public bool allowCancel;
    public Mode mode;

    private double _scroll;
    private double _scrollTarget;
    private UIKey? _lastGpSelection;
    private Dictionary<Vec, int> _gridToIdx = new();
    private Dictionary<int, Vec> _idxToGrid = new();
    private static double _rescoutTimer = 0.0;

    private const int MAX_PER_ROW = 2;
    private const int ARTIFACT_WIDTH = 180;
    private const int ARTIFACT_HEIGHT = 29;
    private const int COL_WIDTH = ARTIFACT_WIDTH + 2;
    private const int ROW_HEIGHT = ARTIFACT_HEIGHT + 2;

    public override bool CanBePeeked() => false;

    public override bool GetShowCockpit() => false;

    public static Vec GridToScreenPos(Vec gridPos)
    {
        return new Vec(422.0 + (gridPos.x - 2.0) * COL_WIDTH, 54.0 + gridPos.y * ROW_HEIGHT);
    }

    public static bool ArtifactIsOnScreen(Vec pos)
    {
        return new Rect(-50.0, -50.0, G.screenSize.x + 100.0, G.screenSize.y + 100.0).Overlaps(
            new Rect(pos.x, pos.y, ARTIFACT_WIDTH, ARTIFACT_HEIGHT));
    }

    public override void Render(G g)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        
        if (_lastGpSelection is not null)
        {
            if (Input.gamepadIsActiveInput) Input.currentGpKey = _lastGpSelection;
            _lastGpSelection = null;
        }

        if (artifactsAvailable.Count == 0)
            g.CloseRoute(this, CBResult.Done);

        // Data preparation
        _gridToIdx.Clear();
        _idxToGrid.Clear();
        for (var i = 0; i < artifactsAvailable.Count; ++i)
        {
            var x = i % MAX_PER_ROW;
            var y = i / MAX_PER_ROW;
            var vec = new Vec(x, y);
            _gridToIdx.Add(vec, i);
            _idxToGrid.Add(i, vec);
        }

        ScrollUtils.ReadScrollInputAndUpdate(
            g.dt,
            (int)Math.Ceiling(artifactsAvailable.Count / (double)MAX_PER_ROW) * ROW_HEIGHT - 162,
            ref _scroll, ref _scrollTarget);

        // Draw static elements
        SharedArt.DrawEngineering(g);
        var screenVec = g.Push(onInputPhase: this).rect.xy;
        Draw.Text(
            string.Format(ModEntry.Instance.Localizations.Localize(mode switch
            {
                Mode.Unlocked => ["cardBrowse", "bootOptionUnlockedArtifactTitle"],
                _ => ["cardBrowse", "eventMissedAPArtifactTitle"]
            }), artifactsAvailable.Count),
            screenVec.x + 240.0, screenVec.y + _scroll + 24.0,
            color: Colors.textMain,
            align: TAlign.Center
        );
        Draw.Text(
            ModEntry.Instance.Localizations.Localize(["cardBrowse", "addArtifactActionDoing"]),
            screenVec.x + 240.0, screenVec.y + _scroll + 34.0,
            color: Colors.textBold,
            maxWidth: 300.0,
            align: TAlign.Center
        );

        // Draw artifact buttons
        var scrollVec = new Vec(y: _scroll);
        for (var i = 0; i < artifactsAvailable.Count; ++i)
        {
            var artifact = artifactsAvailable[i];
            var gridPos = _idxToGrid[i];
            var screenPos = GridToScreenPos(gridPos);

            // Move screen to selected element
            if (Input.gamepadIsActiveInput && Input.currentGpKey == artifact.UIKey())
            {
                _scrollTarget = Math.Clamp(_scrollTarget, -screenPos.y + 60.0, -screenPos.y + 160.0);
            }

            // Only render artifacts onscreen
            if (!ArtifactIsOnScreen(screenPos + scrollVec)) continue;

            // Add d-pad hints for grid movement
            UIKey upHint = StableUK.NO_TARGET;
            UIKey downHint = StableUK.NO_TARGET;
            if (Input.gamepadIsActiveInput && Input.currentGpKey == artifact.UIKey())
            {
                if (_gridToIdx.TryGetValue(gridPos + new Vec(y: -1.0), out var upperIdx))
                    upHint = new UIKey(StableUK.artifactReward_artifact, upperIdx);
                if (_gridToIdx.TryGetValue(gridPos + new Vec(y: 1.0), out var lowerIdx))
                    downHint = new UIKey(StableUK.artifactReward_artifact, lowerIdx);
            }

            // -- RENDER THE SPRITE BUTTON --

            // Encompassing box
            var artifactBox = g.Push(
                new UIKey(StableUK.artifactReward_artifact, i),
                new Rect(screenPos.x + scrollVec.x, screenPos.y + scrollVec.y, ARTIFACT_WIDTH, ARTIFACT_HEIGHT),
                null,
                true,
                onMouseDown: this,
                upHint: upHint,
                downHint: downHint
            );

            var artifactBoxPos = artifactBox.rect.xy;
            var artifactMeta = artifact.GetMeta();
            var isBossArtifact = artifactMeta.pools.Contains(ArtifactPool.Boss);
            var deck = DB.decks[artifactMeta.owner];

            if (isBossArtifact)
            {
                // Glow behind the button
                Draw.Sprite(
                    StableSpr.buttons_artifact_glow,
                    artifactBoxPos.x - 8.0, artifactBoxPos.y - 8.0,
                    color: deck.color.gain(0.5),
                    blend: BlendMode.Screen
                );
            }

            // Background of the button
            Draw.Sprite(
                artifactBox.IsHover() ? StableSpr.buttons_artifact_on : StableSpr.buttons_artifact,
                artifactBoxPos.x, artifactBoxPos.y,
                color: deck.color
            );

            // Tooltip
            if (artifactBox.IsHover())
                g.tooltips.Add(artifactBoxPos + new Vec(ARTIFACT_WIDTH + 3, 2.0), artifact.GetTooltips());

            var offsetPos = artifactBoxPos + new Vec(y: artifactBox.IsHover() ? 1 : 0);
            var spritePos = offsetPos + new Vec(7.0, 7.0);
            artifact.lastScreenPos = spritePos;

            // Icon
            Draw.Sprite(
                artifact.GetSprite(),
                spritePos.x, spritePos.y
            );

            // Title
            Draw.Text(
                ArtifactRewardRenderPatch.GetArtifactName(artifact),
                offsetPos.x + 32.0, offsetPos.y + 7.0,
                color: deck.color,
                outline: Colors.black
            );

            // Subtitle
            var displayName = Character.GetDisplayName(artifactMeta.owner, g.state);
            var baseSubtitle = (artifactMeta.owner != Deck.colorless ? displayName + " " : "")
                               + (isBossArtifact
                                   ? Loc.T("artifactReward.bossArtifactSuffix", "Boss Artifact")
                                   : Loc.T("artifactReward.artifactSuffix", "Artifact"));
            Draw.Text(
                ArtifactRewardRenderPatch.GetArtifactSubtitle(baseSubtitle, artifact),
                offsetPos.x + 32.0, offsetPos.y + 15.0,
                colorForce: deck.color.fadeAlpha(0.4),
                outline: Colors.black
            );

            g.Pop();
        }
        
        // Back button, drawn on top of the rest
        if (GetBackButtonMode() == BackMode.Cancel)
        {
            SharedArt.ButtonText(
                g,
                new Vec(390.0, 228.0),
                StableUK.cardbrowse_cancel,
                Loc.T("uiShared.btnCancel"),
                onMouseDown: this,
                platformButtonHint: Btn.B
            );
        }

        g.Pop();

        if (Archipelago.Instance.APSaveData.CardScoutMode == CardScoutMode.DontScout) return;
            
        // Recheck AP checks every 5 seconds
        if (_rescoutTimer > 5.0)
        {
            _rescoutTimer -= 5.0;
        
            var checkArtifacts = artifactsAvailable
                .Where(artifact => artifact is CheckLocationArtifact)
                .Cast<CheckLocationArtifact>().ToList();
            var locations = checkArtifacts.SelectMany(artifact => artifact.locationName).ToArray();

            if (locations.Length > 0)
            {
                Archipelago.Instance.ScoutLocationInfo(locations).ContinueWith(task =>
                {
                    for (var i = 0; i < checkArtifacts.Count; i++)
                        checkArtifacts[i].LoadInfo(task.Result?.GetSlice(i).ToArray());
                });
            }
        }
        _rescoutTimer += g.dt;
    }

    public void OnInputPhase(G g, Box b)
    {
        if ((Input.GetGpDown(Btn.B) || Input.GetKeyDown(Keys.Escape))
            && GetBackButtonMode() == BackMode.Cancel)
        {
            Audio.Play(FSPRO.Event.Click);
            g.CloseRoute(this, CBResult.Cancel);
        }
    }

    public BackMode GetBackButtonMode() => allowCancel ? BackMode.Cancel : BackMode.None;

    public void OnMouseDown(G g, Box b)
    {
        if (b.key?.ValueFor(StableUK.artifactReward_artifact) is { } idx)
        {
            g.state.SendArtifactToChar(artifactsAvailable[idx]);
            g.CloseRoute(this, CBResult.Done);
            return;
        }

        if (b.key == StableUK.cardbrowse_cancel)
        {
            Audio.Play(FSPRO.Event.Click);
            g.CloseRoute(this, CBResult.Cancel);
            return;
        }
    }
}

[HarmonyPatch(typeof(Dialogue), nameof(Dialogue.TryCloseSubRoute))]
public class DialogueCloseArtifactPickPatch
{
    public static void Prefix(Dialogue __instance, Route r, object? arg)
    {
        if (__instance.routeOverride == r && r is ArtifactPick && arg is CBResult.Cancel)
        {
            __instance.actionQueue.Clear();
        }
    }
}
