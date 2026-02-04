using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CobaltCoreArchipelago.Features;

public static class NotSoRandomManager
{
    private static HashSet<string> RecentlySeenLocations => Archipelago.Instance.APSaveData!.RecentlySeenLocations;
    private static HashSet<string> AllSeenLocations => Archipelago.Instance.APSaveData!.AllSeenLocations;
    
    // Whenever we pick a location, we prevent it from being seen until there are no valid choices left
    internal static string RandomLocation(List<string> validChoices, Rand rng)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        Debug.Assert(validChoices.Count > 0, "validChoices.Count > 0");
        var notSeenChoices = validChoices
            .Except(RecentlySeenLocations)
            .ToList();
        
        // If there are valid choices that haven't been seen yet, we can return an unseen one
        if (notSeenChoices.Count > 0) return notSeenChoices.Random(rng);

        // Otherwise, we put all valid choices "back into the deck"
        RecentlySeenLocations.RemoveWhere(validChoices.Contains);
        return validChoices.Random(rng);
        // Note: we don't set the chosen location as seen immediately
        // because it may not end up actually being seen by the player
    }

    internal static void AddSeenLocation(string location)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        RecentlySeenLocations.Add(location);
        AllSeenLocations.Add(location);
    }

    internal static void AddSeenLocations(IEnumerable<string> locations)
    {
        foreach (var location in locations) AddSeenLocation(location);
    }
}