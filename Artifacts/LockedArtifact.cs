using System;
using System.Collections.Generic;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace CobaltCoreArchipelago.Artifacts;

public class LockedArtifact : Artifact, IRegisterable
{
    internal Artifact? underlying;
    internal string? itemName;

    // Auto-recreate an instance of the artifact if it was lost (if the game was closed for example)
    internal Artifact? UnderlyingArtifact
    {
        get
        {
            if (underlying is not null) return underlying;
            if (itemName is null) return null;
            underlying = Archipelago.ItemToArtifact[itemName].CreateInstance() as Artifact;
            return underlying;
        }
    }
    
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
        if (UnderlyingArtifact is null) return null;

        List<Tooltip> underlyingInfo =
        [
            new TTText("<c=artifact>" + UnderlyingArtifact.Name() + "</c>"),
            new TTText(UnderlyingArtifact.Description())
        ];
        var underlyingTooltips = UnderlyingArtifact.GetExtraTooltips();
        if (underlyingTooltips is null)
            return underlyingInfo;
        
        underlyingInfo.Add(new TTDivider());
        underlyingInfo.AddRange(underlyingTooltips);
        return underlyingInfo;
    }

    internal void SetUnderlyingArtifact(Artifact artifact)
    {
        underlying = artifact;
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