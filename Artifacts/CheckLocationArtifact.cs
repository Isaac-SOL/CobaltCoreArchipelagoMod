using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Archipelago.MultiClient.Net.Models;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace CobaltCoreArchipelago.Artifacts;

public class CheckLocationArtifact : Artifact, IRegisterable
{
    [JsonIgnore]
    public const int MaxItems = 2;
    [JsonIgnore]
    public static Spr BaseSpr;
    
    // Note : fields MUST be public if the artifact is copied somewhere or saved
    public string?[] locationName = ["", null];
    public string?[] locationSlotName = [null, null];
    public string?[] locationItemName = [null, null];
    public string?[] locationItemColor = [null, null];
    public string?[] givenCard = [null, null];
    public string?[] givenArtifact = [null, null];
    
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
        Archipelago.Instance.CheckLocation(locationName[0]!);
        if (IsDouble()) Archipelago.Instance.CheckLocation(locationName[1]!);
    }
    
    private static string Localize(params string[] key) =>
        ModEntry.Instance.Localizations.Localize(new List<string>{"artifact", "CheckLocationArtifact"}.Concat(key).ToArray());

    public override List<Tooltip>? GetExtraTooltips()
    {
        var itemTooltips = Enumerable.Range(0, MaxItems)
            .Select(GetExtraTooltipsForItem)
            .Where(l => l is not null)
            .SelectMany(l => l!.Append(new TTDivider()))
            .SkipLast(1)
            .ToList();
        return itemTooltips.Count > 0 ? itemTooltips : null;
    }

    public List<Tooltip>? GetExtraTooltipsForItem(int pos)
    {
        if (locationItemName[pos] is null)
            return null;

        string description;
        if (IsLocal(pos))
        {
            var state = MG.inst.g.state;  // This is really ugly but my hands were tied
            description = Localize(WillAddCardToDeck(state, pos)
                                       ? "descSelfAddCard"
                                       : WillAddArtifact(state, pos)
                                           ? "descSelfAddArtifact"
                                           : "descSelf");
            description = string.Format(description, locationItemName[pos]);
        }
        else
        {
            description = Localize("descBase");
            description = string.Format(description,
                                        $"<c={locationItemColor[pos]}>{locationItemName[pos]}</c>",
                                        $"<c={APColors.OtherPlayer}>{locationSlotName[pos]}</c>");
        }
        List<Tooltip> tooltips = [new TTText(description)];
        
        if (givenCard[pos] != null)
        {
            tooltips.Add(new TTCard
            {
                card = (Card) Archipelago.ItemToCard[givenCard[pos]!].CreateInstance()
            });
        }
        
        if (givenArtifact[pos] != null)
        {
            var artifact = (Artifact) Archipelago.ItemToArtifact[givenArtifact[pos]!].CreateInstance();
            tooltips.Add(new TTDivider());
            tooltips.AddRange(artifact.GetTooltips());
        }
            
        return tooltips;
    }
    
    private bool IsLocal(int pos)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        return locationSlotName[pos] == Archipelago.Instance.APSaveData.Slot;
    }

    private bool IsDouble() => locationName[1] is not null;

    private bool HasDeck(State state, int pos) =>
        (givenCard[pos] is not null
         && Archipelago.ItemToCard.TryGetValue(givenCard[pos]!, out var cardType)
         && DB.cardMetas.TryGetValue(cardType.Name, out var cardMeta)
         && state.characters.Any(character => character.deckType == cardMeta.deck))
        || (givenArtifact[pos] is not null
            && Archipelago.ItemToArtifact.TryGetValue(givenArtifact[pos]!, out var artifactType)
            && DB.artifactMetas.TryGetValue(artifactType.Name, out var artifactMeta)
            && state.characters.Any(character => character.deckType == artifactMeta.owner));

    private bool WillAddCardToDeck(State state, int pos)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        if (givenCard[pos] is null) return false;
        if (!IsLocal(pos)) return false;
        if (Archipelago.Instance.APSaveData.HasItem(givenCard[pos]!)) return false;
        return Archipelago.InstanceSlotData.ImmediateCardRewards switch
        {
            CardRewardsMode.Always or CardRewardsMode.IfLocal => true,
            CardRewardsMode.IfHasDeck or CardRewardsMode.IfLocalAndHasDeck => HasDeck(state, pos),
            _ => false
        };
    }

    private bool WillAddArtifact(State state, int pos)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        if (givenArtifact[pos] is null) return false;
        if (!IsLocal(pos)) return false;
        if (Archipelago.Instance.APSaveData.HasItem(givenArtifact[pos]!)) return false;
        return Archipelago.InstanceSlotData.ImmediateArtifactRewards switch
        {
            CardRewardsMode.Always or CardRewardsMode.IfLocal => true,
            CardRewardsMode.IfHasDeck or CardRewardsMode.IfLocalAndHasDeck => HasDeck(state, pos),
            _ => false
        };
    }

    internal void SetTextInfo(string itemName, string slotName, string itemColor, int pos = 0)
    {
        locationItemName[pos] = itemName;
        locationSlotName[pos] = slotName;
        locationItemColor[pos] = itemColor;
        if (IsLocal(pos))
        {
            if (Archipelago.ItemToCard.ContainsKey(locationItemName[pos]!))
                givenCard[pos] = locationItemName[pos];
            else if (Archipelago.ItemToArtifact.ContainsKey(locationItemName[pos]!))
                givenArtifact[pos] = locationItemName[pos];
        }
    }

    internal void LoadInfo(ScoutedItemInfo?[]? infos)
    {
        if (infos is null)
            SetTextInfo("[]", "[]", APColors.Trap);
        else
            for (var i = 0; i < infos.Length; i++)
                if (infos[i] is { } info)
                    SetTextInfo(info.ItemName, info.Player.Name, info.GetColor(), pos: i);
    }
}

public class CheckLocationArtifactBoss : CheckLocationArtifact
{
    public new static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
    {
        RegisterWithPool(package, helper, ArtifactPool.Boss, typeof(CheckLocationArtifactBoss));
    }
}

public static class CheckLocationArtifactExtensions
{
    internal static IEnumerable<ScoutedItemInfo?> GetSlice(this IEnumerable<ScoutedItemInfo?> allInfos, int artifactNum)
    {
        return allInfos.ToList()
            .GetRange(artifactNum * CheckLocationArtifact.MaxItems, CheckLocationArtifact.MaxItems)
            .ToArray();
    }
}