using System.Collections.Generic;
using HarmonyLib;

namespace CobaltCoreArchipelago.Features;

// This class holds instances of artifacts to be used by LockedArtifact,
// so that LockedArtifact doesn't have to hold them (seemingly prevents them from being saved correctly?)
public static class LockedArtifactInstanceCache
{
    private static Dictionary<string, Artifact> LockedArtifacts = new();

    internal static Artifact? Get(string name)
    {
        if (LockedArtifacts.TryGetValue(name, out var artifact)) return artifact;
        if (!Archipelago.ItemToArtifact.TryGetValue(name, out var artifactType)) return null;
        var newArtifact = (Artifact)artifactType.CreateInstance();
        LockedArtifacts[name] = newArtifact;
        return newArtifact;
    }
}