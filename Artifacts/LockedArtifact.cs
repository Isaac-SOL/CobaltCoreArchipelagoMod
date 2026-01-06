using System;
using System.Collections.Generic;
using CobaltCoreArchipelago.Features;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace CobaltCoreArchipelago.Artifacts;

public class LockedArtifact : Artifact, IRegisterable
{
    public string? itemName;

    // Auto-recreate an instance of the artifact if it was lost (if the game was closed for example)
    internal Artifact? UnderlyingArtifact => itemName is null ? null : LockedArtifactInstanceCache.Get(itemName);

    public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
    {
        RegisterWithPool(package, helper, ArtifactPool.Common, typeof(LockedArtifact));
    }

    internal static void RegisterWithPool(IPluginPackage<IModManifest> package, IModHelper helper, ArtifactPool pool,
                                          Type artifactType)
    {
        helper.Content.Artifacts.RegisterArtifact(new ArtifactConfiguration
        {
            ArtifactType = artifactType,
            Meta = new ArtifactMeta
            {
                pools = [pool],
                owner = ModEntry.Instance.LockedDeck.Deck
            },
            Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "LockedArtifact", "name"]).Localize,
            Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "LockedArtifact", "desc"]).Localize,
            Sprite = StableSpr.artifacts_Unknown
        });
    }

    public override Spr GetSprite()
    {
        return UnderlyingArtifact?.GetSprite() ?? StableSpr.artifacts_Unknown;
    }

    public override List<Tooltip>? GetExtraTooltips()
    {
        return UnderlyingArtifact is null
            ? [new TTText(ModEntry.Instance.Localizations.Localize(["artifact", "LockedArtifact", "descNotFound"]))]
            : UnderlyingArtifact.GetTooltips();
    }

    internal void SetUnderlyingArtifact(Artifact artifact)
    {
        itemName = Archipelago.ArtifactToItem[artifact.GetType()];
    }
}

public class LockedArtifactBoss : LockedArtifact
{
    public new static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
    {
        RegisterWithPool(package, helper, ArtifactPool.Boss, typeof(LockedArtifactBoss));
    }
}
