using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace CobaltCoreArchipelago.Artifacts;

public class CheckLocationArtifact : Artifact, IRegisterable
{
    [JsonIgnore]
    public static Spr BaseSpr;
    
    public string locationName = "";
    public string? locationSlotName;
    public string? locationItemName;
    public string? givenCard;
    public string? givenArtifact;
    
    public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
    {
        RegisterWithPool(package, helper, ArtifactPool.Common, typeof(CheckLocationArtifact));
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
                owner = ModEntry.Instance.ArchipelagoDeck.Deck
            },
            Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "CheckLocationArtifact", "name"]).Localize,
            Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "CheckLocationArtifact", "descNotFound"]).Localize,
            /*
             * For Artifacts with just one sprite, registering them at the place of usage helps simplify things.
             */
            Sprite = BaseSpr
        });
    }

    public override void OnReceiveArtifact(State state)
    {
        // Do the check, then remove the artifact
        Archipelago.Instance.CheckLocation(locationName);
    }

    public override List<Tooltip>? GetExtraTooltips()
    {
        if (locationItemName is null)
            return null;

        string description;
        if (IsLocal())
        {
            description = ModEntry.Instance.Localizations.Localize(
                ["card", "CheckLocationCard", "descSelf"]);
            description = string.Format(description, locationItemName);
        }
        else
        {
            description = ModEntry.Instance.Localizations.Localize(
                ["card", "CheckLocationCard", "descBase"]);
            description = string.Format(description, locationItemName, locationSlotName);
        }
        List<Tooltip> tooltips = [new TTText(description)];
        
        if (givenCard != null)
        {
            tooltips.Add(new TTCard
            {
                card = (Card) Archipelago.ItemToCard[givenCard].CreateInstance()
            });
        }
        
        if (givenArtifact != null)
        {
            var artifact = (Artifact) Archipelago.ItemToArtifact[givenArtifact].CreateInstance();
            tooltips.Add(new TTDivider());
            tooltips.AddRange(artifact.GetTooltips());
        }
            
        return tooltips;
    }
    
    private bool IsLocal()
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        return locationSlotName == Archipelago.Instance.APSaveData.Slot;
    }

    internal void SetTextInfo(string itemName, string slotName)
    {
        locationItemName = itemName;
        locationSlotName = slotName;
        if (IsLocal())
        {
            if (Archipelago.ItemToCard.ContainsKey(locationItemName))
                givenCard = locationItemName;
            else if (Archipelago.ItemToArtifact.ContainsKey(locationItemName))
                givenArtifact = locationItemName;
        }
    }
}

public class CheckLocationArtifactBoss : CheckLocationArtifact
{
    public new static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
    {
        RegisterWithPool(package, helper, ArtifactPool.Boss, typeof(CheckLocationArtifactBoss));
    }
}