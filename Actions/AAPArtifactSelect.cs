using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CobaltCoreArchipelago.Artifacts;
using CobaltCoreArchipelago.Features;
using CobaltCoreArchipelago.GameplayPatches;
using HarmonyLib;
using Microsoft.Extensions.Logging;

namespace CobaltCoreArchipelago.Actions;

public class AAPArtifactSelect : CardAction
{
    public required ArtifactPick.Mode mode;
    public bool allowCancel;

    public override bool CanSkipTimerIfLastEvent() => false;
    
    public override Route? BeginWithRoute(G g, State s, Combat c)
    {
        Debug.Assert(Archipelago.Instance.Session != null, "Archipelago.Instance.Session != null");
        var allMissingLocations = Archipelago.Instance.Session.Locations.AllMissingLocations
            .Select(l => Archipelago.Instance.Session.Locations.GetLocationNameFromId(l))
            .ToList();

        var artifacts = mode switch
        {
            ArtifactPick.Mode.Unlocked => CardBrowseListPatch.GetPickableUnlockedArtifactsList(s),
            _ => CardBrowseListPatch.GetPickableAPArtifactsList(s)
                .Select(loc =>
                {
                    string? loc2 = null;
                    // If shuffle mode is double, only odd numbers appear in seen locations.
                    // In that case we try to attach a second location
                    if (Archipelago.InstanceSlotData.ShuffleArtifacts == ArtifactShuffleMode.Double)
                    {
                        var locParts = loc.Split(' ');
                        if (int.TryParse(locParts.Last(), out var idx) && idx % 2 == 1)
                        {
                            var possibleSecondLocation = loc[..^locParts.Last().Length] + (idx + 1);
                            if (allMissingLocations.Contains(possibleSecondLocation))
                                loc2 = possibleSecondLocation;
                        }
                    }
                    var artifact = loc.Contains("Boss")
                        ? new CheckLocationArtifactBoss()
                        : new CheckLocationArtifact();
                    artifact.locationName = [loc, loc2];
                    return artifact;
                })
                .Cast<Artifact>()
                .ToList()
        };
        
        c.Queue(new ADelay
        {
            time = 0.0,
            timer = 0.0
        });

        if (artifacts.Count == 0)
        {
            timer = 0.0;
            return null;
        }

        if (mode == ArtifactPick.Mode.MissedAP
            && Archipelago.InstanceSlotData.ShuffleArtifacts == ArtifactShuffleMode.Double)
        {
            var checkArtifacts = artifacts.Cast<CheckLocationArtifact>().ToList();
            var locations = checkArtifacts.SelectMany(artifact => artifact.locationName).ToArray();
            Archipelago.Instance.ScoutLocationInfo(locations).ContinueWith(task =>
            {
                for (var i = 0; i < checkArtifacts.Count; i++)
                    checkArtifacts[i].LoadInfo(task.Result?.GetSlice(i).ToArray());
            });
        }

        return new ArtifactPick
        {
            mode = mode,
            allowCancel = allowCancel,
            artifactsAvailable = artifacts
        };
    }

    public override List<Tooltip> GetTooltips(State s) =>
    [
        new TTGlossary("action.searchCardNew", mode switch
        {
            ArtifactPick.Mode.Unlocked =>
                ModEntry.Instance.Localizations.Localize(["cardBrowse", "bootOptionUnlockedArtifactDesc"]),
            ArtifactPick.Mode.MissedAP =>
                ModEntry.Instance.Localizations.Localize(["cardBrowse", "eventMissedAPArtifactDesc"]),
            _ => "!!!missing string!!!"
        })
    ];

    public override Icon? GetIcon(State s)
    {
        return new Icon(StableSpr.icons_searchCard, null, Colors.textMain);
    }
}