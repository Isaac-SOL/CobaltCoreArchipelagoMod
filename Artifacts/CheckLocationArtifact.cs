using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    
    // Note : fields MUST be public if the artifact is copied somewhere or saved
    public string locationName = "";
    public string? locationSlotName;
    public string? locationItemName;
    public string? locationItemColor;
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
        Archipelago.Instance.CheckLocation(locationName);
    }
    
    private static string Localize(params string[] key) =>
        ModEntry.Instance.Localizations.Localize(new List<string>{"artifact", "CheckLocationArtifact"}.Concat(key).ToArray());

    public override List<Tooltip>? GetExtraTooltips()
    {
        if (locationItemName is null)
            return null;

        string description;
        if (IsLocal())
        {
            var state = MG.inst.g.state;  // This is really ugly but my hands were tied
            description = Localize(WillAddCardToDeck(state)
                                       ? "descSelfAddCard"
                                       : WillAddArtifact(state)
                                           ? "descSelfAddArtifact"
                                           : "descSelf");
            description = string.Format(description, locationItemName);
        }
        else
        {
            description = Localize("descBase");
            description = string.Format(description,
                                        $"<c={locationItemColor}>{locationItemName}</c>",
                                        $"<c={APColors.OtherPlayer}>{locationSlotName}</c>");
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

    private bool HasDeck(State state) =>
        (givenCard is not null && DB.cardMetas.TryGetValue(Archipelago.ItemToCard[givenCard].Name, out var cardMeta)
                               && state.characters.Any(character => character.deckType == cardMeta.deck))
        || (givenArtifact is not null && DB.artifactMetas.TryGetValue(givenArtifact, out var artifactMeta)
                                      && state.characters.Any(character => character.deckType == artifactMeta.owner));

    private bool WillAddCardToDeck(State state)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        if (givenCard is null) return false;
        if (!IsLocal()) return false;
        if (Archipelago.Instance.APSaveData.HasItem(givenCard)) return false;
        return Archipelago.InstanceSlotData.ImmediateCardRewards switch
        {
            CardRewardsMode.Always or CardRewardsMode.IfLocal => true,
            CardRewardsMode.IfHasDeck or CardRewardsMode.IfLocalAndHasDeck => HasDeck(state),
            _ => false
        };
    }

    private bool WillAddArtifact(State state)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        if (givenArtifact is null) return false;
        if (!IsLocal()) return false;
        if (Archipelago.Instance.APSaveData.HasItem(givenArtifact)) return false;
        return Archipelago.InstanceSlotData.ImmediateArtifactRewards switch
        {
            CardRewardsMode.Always or CardRewardsMode.IfLocal => true,
            CardRewardsMode.IfHasDeck or CardRewardsMode.IfLocalAndHasDeck => HasDeck(state),
            _ => false
        };
    }

    internal void SetTextInfo(string itemName, string slotName, string itemColor)
    {
        locationItemName = itemName;
        locationSlotName = slotName;
        locationItemColor = itemColor;
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