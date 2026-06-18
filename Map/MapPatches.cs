using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HarmonyLib;
using Microsoft.Extensions.Logging;

namespace CobaltCoreArchipelago.Map;

[HarmonyPatch(typeof(MapBase), nameof(MapBase.Populate))]
public class MapPatches
{
    private static readonly List<(int x, int y)> positions =
    [
        (4, 2), (5, 2), (3, 2), (4, 1), (4, 3)
    ];
    
    public static void Postfix(MapBase __instance, State s, Rand rng)
    {
        if (!Archipelago.InstanceSlotData.SwapCharacterNode) return;
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        // Don't add the node if we have no additional unlocked characters
        if (!Archipelago.Instance.APSaveData.FoundChars.Any(d => s.characters.All(c => c.deckType != d)))
            return;
        
        // First, we try to add a new node in an unoccupied space
        foreach (var pos in positions)
        {
            var (valid, prev, next) = IsPositionValid(__instance, pos);
            if (!valid) continue;
            ModEntry.Instance.Logger.LogInformation("Swap Character Node: Valid empty position found at {pos}", pos);
            ModEntry.Instance.Logger.LogInformation("Prev: [{prev}], next: [{next}]", prev.Select(VtT), next.Select(VtT));
            
            // Add the node to the map
            Marker marker = new();
            __instance.markers[TtV(pos)] = marker;
            marker.contents = new MapSwapCharacter();
            
            // Pick & add paths
            var prevPathAmount = prev.Count > 1 ? (1 + rng.NextUint() % 2) : 1;
            var nextPathAmount = next.Count > 1 ? (1 + rng.NextUint() % 2) : 1;
            var prevPaths = prev.Shuffle().Take((int)prevPathAmount).ToList();
            var nextPaths = next.Shuffle().Take((int)nextPathAmount).ToList();
            foreach (var path in prevPaths)
                __instance.markers[path].paths.Add(pos.y);
            foreach (var path in nextPaths)
                marker.paths.Add(VtT(path).y);

            return;
        }
        
        ModEntry.Instance.Logger.LogInformation("Swap Character Node: No empty position found.");

        // If all 5 positions are invalid we instead try to replace an existing node altogether
        foreach (var pos in positions)
        {
            if (!__instance.markers.TryGetValue(TtV(pos), out var oldMarker)) continue;
            ModEntry.Instance.Logger.LogInformation("Swap Character Node: Valid replacement position found at {pos}", pos);
            ModEntry.Instance.Logger.LogInformation("Borrowing next paths: [{paths}]", oldMarker.paths);
            
            var paths = oldMarker.paths;
            Marker marker = new();
            __instance.markers[TtV(pos)] = marker;
            marker.contents = new MapSwapCharacter();
            marker.paths = paths;
            return;
        }
    }

    private static (bool valid, List<Vec> prev, List<Vec> next) IsPositionValid(MapBase map, (int x, int y) pos)
    {
        // If there is something at this position, it is invalid
        if (map.markers.ContainsKey(TtV(pos))) return (false, [], []);
        // Otherwise we check that there are enough valid paths that can be built before and after
        List<(int x, int y)> prevMarkers =
        [
            (pos.x - 1, pos.y - 1),
            (pos.x - 1, pos.y),
            (pos.x - 1, pos.y + 1)
        ];
        List<(int x, int y)> nextMarkers =
        [
            (pos.x + 1, pos.y - 1),
            (pos.x + 1, pos.y),
            (pos.x + 1, pos.y + 1)
        ];
        var validPrev = prevMarkers
            .Where(prevPos => map.markers.ContainsKey(TtV(prevPos))
                              && CanConstructPath(map, prevPos, pos))
            .Select(TtV)
            .ToList();
        var validNext = nextMarkers
            .Where(nextPos => map.markers.ContainsKey(TtV(nextPos))
                              && CanConstructPath(map, pos, nextPos))
            .Select(TtV)
            .ToList();
        return (validPrev.Count > 0 && validNext.Count > 0, validPrev, validNext);
    }

    // x and y are swapped because that works better in my mind
    private static Vec TtV((int x, int y) tuple) => new (tuple.y, tuple.x);

    private static (int x, int y) VtT(Vec vec) => ((int)vec.y, (int)vec.x);
    
    // Only works correctly if pos1 is one of the 3 positions behind pos2
    private static bool CanConstructPath(MapBase map, (int x, int y) pos1, (int x, int y) pos2)
    {
        // If aligned, path can be made
        if (pos1.y == pos2.y) return true;
        // If diagonal, we check if there's a crossing diagonal
        if (pos1.y > pos2.y)
        {
            var posA = pos1 with { y = pos1.y - 1 };
            var posB = pos2 with { y = pos2.y + 1 };
            if (map.markers.TryGetValue(TtV(posA), out var markerA)
                && map.markers.ContainsKey(TtV(posB))
                && markerA.paths.Contains(posB.y))
                return false;
        }
        else // (pos1.y < pos2.y)
        {
            var posA = pos1 with { y = pos1.y + 1 };
            var posB = pos2 with { y = pos2.y - 1 };
            if (map.markers.TryGetValue(TtV(posA), out var markerA)
                && map.markers.ContainsKey(TtV(posB))
                && markerA.paths.Contains(posB.y))
                return false;
        }
        // No crossing diagonals, path is clear
        return true;
    }
}