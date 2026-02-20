using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Archipelago.MultiClient.Net.Helpers;
using Microsoft.Xna.Framework.Input;

namespace CobaltCoreArchipelago;

public class Tracker : Route, OnInputPhase, OnMouseDown
{
    private const uint OutOfLogicColor = 0xFF884444, CompletedColor = 0xFF555555;
    
    private double scroll;
    private double scrollTarget;
    private Dictionary<Deck, CharacterLocationsSummary> summaryCache;
    private Dictionary<string, (string item, string player)> hintsCache = [];

    private static IHintsHelper Hints => Archipelago.Instance.Session!.Hints;
    
    internal int GetMaxScrollLength() => 150;

    public Tracker()
    {
        Debug.Assert(Archipelago.Instance.Session != null, "Archipelago.Instance.Session != null");
        summaryCache = CharacterLocationsSummary.GetLocationsSummary();
        Hints.GetHintsAsync().ContinueWith(task =>
        {
            if (task.Result is null) return;
            var locations = Archipelago.Instance.Session.Locations;
            var items = Archipelago.Instance.Session.Items;
            var players = Archipelago.Instance.Session.Players;
            lock (hintsCache)
            {
                task.Result.ToList().ForEach(hint =>
                {
                    if (hint.FindingPlayer != Archipelago.Instance.Session.ConnectionInfo.Slot) return;
                    if (locations.AllLocationsChecked.Contains(hint.LocationId)) return;
                    hintsCache[locations.GetLocationNameFromId(hint.LocationId)] = (
                        items.GetItemName(hint.ItemId, players.GetPlayerInfo(hint.ReceivingPlayer).Game),
                        players.GetPlayerName(hint.ReceivingPlayer)
                    );
                });
            }
        });
    }

    private IEnumerable<(string item, string player)> GetHints(Dictionary<string, bool> locationGroup)
    {
        lock (hintsCache)
        {
            return hintsCache
                .Where(kvp => locationGroup.TryGetValue(kvp.Key, out var wasChecked) && !wasChecked)
                .Select(kvp => kvp.Value);
        }
    }

    private IEnumerable<Tooltip> GetHintTooltips(Dictionary<string, bool> locationGroup)
    {
        return GetHints(locationGroup).Select(pair => new TTText($"{pair.item} for {pair.player}"));
    }

    public override void Render(G g)
    {
        SharedArt.DrawEngineering(g);
        
        ScrollUtils.ReadScrollInputAndUpdate(g.dt, GetMaxScrollLength(), ref scroll, ref scrollTarget);
        if (Input.gamepadIsActiveInput && Input.currentGpKey.HasValue)
        {
            scrollTarget = Mutil.Clamp(scrollTarget, 20.0 - GetMaxScrollLength(), 120.0 - GetMaxScrollLength());
        }
        
        Draw.Text("Archipelago Tracker", 111, 15.0 + scroll, DB.stapler, Colors.textMain);

        if (Archipelago.InstanceSlotData.ShuffleArtifacts != ArtifactShuffleMode.Off)
        {
            var basicCommonArtifactCount = summaryCache[Deck.tooth].Artifacts.Common.Count(kvp => kvp.Value);
            var basicBossArtifactCount = summaryCache[Deck.tooth].Artifacts.Boss.Count(kvp => kvp.Value);
            var basicArtifactCount = basicCommonArtifactCount + basicBossArtifactCount;
        
            var basicCommonArtifactAll = summaryCache[Deck.tooth].Artifacts.Common.Count;
            var basicBossArtifactAll = summaryCache[Deck.tooth].Artifacts.Boss.Count;
            var basicArtifactAll = basicCommonArtifactAll + basicBossArtifactAll;

            var basicArtifactsBox = g.Push(rect: new Rect(170, 42 + scroll, 150, 8),
                                           key: new UIKey(ArchipelagoUK.codex_charArtifacts.ToUK(), 0));
            var hintCount = GetHints(summaryCache[Deck.tooth].AllArtifacts).Count();
            Draw.Text(
                $"Basic Artifacts: {basicArtifactCount}/{basicArtifactAll}"
                + (hintCount > 0 ? $"     <c=card>({hintCount} hinted)</c>" : ""),
                basicArtifactsBox.rect.x, basicArtifactsBox.rect.y + 1,
                color: basicArtifactCount < basicArtifactAll ? Colors.white : CompletedColor
            );
            if (basicArtifactsBox.IsHover())
            {
                var tooltips = new List<Tooltip>
                {
                    new TTText("<c=artifact>BASIC ARTIFACTS</c>\n" +
                               $"<c=card>Common: {basicCommonArtifactCount}/{basicCommonArtifactAll}\n</c>")
                };
                tooltips.AddRange(GetHintTooltips(summaryCache[Deck.tooth].Artifacts.Common));
                tooltips.AddRange( new List<Tooltip>{
                    new TTDivider(),
                    new TTText($"<c=card>Boss: {basicBossArtifactCount}/{basicBossArtifactAll}\n</c>")
                });
                tooltips.AddRange(GetHintTooltips(summaryCache[Deck.tooth].Artifacts.Boss));
                g.tooltips.Add(basicArtifactsBox.rect.xy + new Vec(-152, -2), tooltips);
            }
            g.Pop();
        }

        var offset = scroll;
        var ukOffset = 1;
        foreach (var deck in Archipelago.DeckToItem.Keys)
        {
            var character = new Character
            {
                type = deck.Key(),
                deckType = deck
            };
            var unlocked = g.state.storyVars.GetUnlockedChars().Contains(deck);

            character.Render(
                g,
                130, (int)(60 + offset),
                mini: true,
                renderLocked: !unlocked
            );
            
            var charColor = unlocked ? Colors.LookupColor(deck.Key()) : OutOfLogicColor;

            Draw.Text(
                Character.GetDisplayName(deck, g.state).ToUpper(),
                170, 62 + offset,
                color: charColor,
                outline: Colors.black
            );

            var commonCardCount =     summaryCache[deck].Cards.Common.Count(kvp => kvp.Value);
            var uncommonCardCount =   summaryCache[deck].Cards.Uncommon.Count(kvp => kvp.Value);
            var rareCardCount =       summaryCache[deck].Cards.Rare.Count(kvp => kvp.Value);
            var commonArtifactCount = summaryCache[deck].Artifacts.Common.Count(kvp => kvp.Value);
            var bossArtifactCount =   summaryCache[deck].Artifacts.Boss.Count(kvp => kvp.Value);
            var cardCount = commonCardCount + uncommonCardCount + rareCardCount;
            var artifactCount = commonArtifactCount + bossArtifactCount;
            var memoryCount = summaryCache[deck].Memories.Count(kvp => kvp.Value);
            
            var commonCardAll =     summaryCache[deck].Cards.Common.Count;
            var uncommonCardAll =   summaryCache[deck].Cards.Uncommon.Count;
            var rareCardAll =       summaryCache[deck].Cards.Rare.Count;
            var commonArtifactAll = summaryCache[deck].Artifacts.Common.Count;
            var bossArtifactAll =   summaryCache[deck].Artifacts.Boss.Count;
            var cardAll = commonCardAll + uncommonCardAll + rareCardAll;
            var artifactAll = commonArtifactAll + bossArtifactAll;
            var memoryAll = summaryCache[deck].Memories.Count;

            var subOffset = 71;

            if (Archipelago.InstanceSlotData.ShuffleCards)
            {
                var cardsBox = g.Push(rect: new Rect(170, subOffset + offset, 150, 8),
                                      key: new UIKey(ArchipelagoUK.codex_charCards.ToUK(), ukOffset));
                var hintCount = GetHints(summaryCache[deck].AllCards).Count();
                Draw.Text(
                    $"Cards: {cardCount}/{cardAll}"
                    + (hintCount > 0 ? $"     <c=card>({hintCount} hinted)</c>" : ""),
                    cardsBox.rect.x, cardsBox.rect.y + 1,
                    color: cardCount < cardAll ? charColor : CompletedColor
                );
                if (cardsBox.IsHover())
                {
                    var tooltips = new List<Tooltip>
                    {
                        new TTText($"<c=artifact>{Character.GetDisplayName(deck, g.state).ToUpper()} CARDS</c>\n" +
                                   $"<c=card>Common: {commonCardCount}/{commonCardAll}\n</c>")
                    };
                    tooltips.AddRange(GetHintTooltips(summaryCache[deck].Cards.Common));
                    tooltips.AddRange( new List<Tooltip>{
                        new TTDivider(),
                        new TTText($"<c=card>Uncommon: {uncommonCardCount}/{uncommonCardAll}\n</c>")
                    });
                    tooltips.AddRange(GetHintTooltips(summaryCache[deck].Cards.Uncommon));
                    tooltips.AddRange( new List<Tooltip>{
                        new TTDivider(),
                        new TTText($"<c=card>Rare: {rareCardCount}/{rareCardAll}\n</c>")
                    });
                    tooltips.AddRange(GetHintTooltips(summaryCache[deck].Cards.Rare));
                    g.tooltips.Add(cardsBox.rect.xy + new Vec(-152, -2), tooltips);
                }
                g.Pop();
                subOffset += 10;
            }

            if (Archipelago.InstanceSlotData.ShuffleArtifacts != ArtifactShuffleMode.Off)
            {
                var artifactsBox = g.Push(rect: new Rect(170, subOffset + offset, 150, 8),
                                          key: new UIKey(ArchipelagoUK.codex_charArtifacts.ToUK(), ukOffset));
                var hintCount = GetHints(summaryCache[deck].AllArtifacts).Count();
                Draw.Text(
                    $"Artifacts: {artifactCount}/{artifactAll}"
                    + (hintCount > 0 ? $"     <c=card>({hintCount} hinted)</c>" : ""),
                    artifactsBox.rect.x, artifactsBox.rect.y + 1,
                    color: artifactCount < artifactAll ? charColor : CompletedColor
                );
                if (artifactsBox.IsHover())
                {
                    var tooltips = new List<Tooltip>
                    {
                        new TTText($"<c=artifact>{Character.GetDisplayName(deck, g.state).ToUpper()} ARTIFACTS</c>\n" +
                                   $"<c=card>Common: {commonArtifactCount}/{commonArtifactAll}\n</c>")
                    };
                    tooltips.AddRange(GetHintTooltips(summaryCache[deck].Artifacts.Common));
                    tooltips.AddRange(new List<Tooltip>
                    {
                        new TTDivider(),
                        new TTText($"<c=card>Boss: {bossArtifactCount}/{bossArtifactAll}\n</c>")
                    });
                    tooltips.AddRange(GetHintTooltips(summaryCache[deck].Artifacts.Boss));
                    g.tooltips.Add(artifactsBox.rect.xy + new Vec(-152, -2), tooltips);
                }
                g.Pop();
                subOffset += 10;
            }

            if (Archipelago.InstanceSlotData.ShuffleMemories)
            {
                var memoriesBox = g.Push(rect: new Rect(170, subOffset + offset, 150, 8),
                                         key: new UIKey(ArchipelagoUK.codex_charMemories.ToUK(), ukOffset));
                var hintCount = GetHints(summaryCache[deck].Memories).Count();
                Draw.Text(
                    $"Memory unlocks: {memoryCount}/{memoryAll}"
                    + (hintCount > 0 ? $"     <c=card>({hintCount} hinted)</c>" : ""),
                    memoriesBox.rect.x, memoriesBox.rect.y + 1,
                    color: memoryCount < memoryAll ? charColor : CompletedColor
                );
                if (memoriesBox.IsHover())
                {
                    var tooltips = new List<Tooltip>
                    {
                        new TTText($"<c=artifact>{Character.GetDisplayName(deck, g.state).ToUpper()} MEMORIES</c>\n" +
                                   $"All: {memoryCount}/{memoryAll}")
                    };
                    tooltips.AddRange(GetHintTooltips(summaryCache[deck].Memories));
                    g.tooltips.Add(memoriesBox.rect.xy + new Vec(-152, -2), tooltips);
                }
                g.Pop();
                subOffset += 10;
            }

            offset += 45;
            ukOffset++;
        }

        SharedArt.ButtonText(
            g,
            new Vec(413.0, 228.0),
            StableUK.codex_back,
            Loc.T("uiShared.btnBack"),
            onMouseDown: this,
            platformButtonHint: Btn.B
        );

    }

    public void OnInputPhase(G g, Box b)
    {
        if (Input.GetGpDown(Btn.B) || Input.GetKeyDown(Keys.Escape))
            g.CloseRoute(this);
    }

    public void OnMouseDown(G g, Box b)
    {
        if (b.key == StableUK.codex_back)
        {
            Audio.Play(FSPRO.Event.Click);
            g.CloseRoute(this);
        }
    }
}

internal class CharacterLocationsSummary
{
    internal (Dictionary<string, bool> Common, Dictionary<string, bool> Uncommon, Dictionary<string, bool> Rare) Cards;
    internal Dictionary<string, bool> AllCards = [];
    internal (Dictionary<string, bool> Common, Dictionary<string, bool> Boss) Artifacts;
    internal Dictionary<string, bool> AllArtifacts = [];
    internal Dictionary<string, bool> Memories = [];

    private static ILocationCheckHelper Locations => Archipelago.Instance.Session!.Locations;

    internal static Dictionary<Deck, CharacterLocationsSummary> GetLocationsSummary()
    {
        Debug.Assert(Archipelago.Instance.Session != null, "Archipelago.Instance.Session != null");
        var allLocations = Locations.AllLocations
            .Select(l => Locations.GetLocationNameFromId(l))
            .ToHashSet();
        var allCheckedLocations = Locations.AllLocationsChecked
            .Select(l => Locations.GetLocationNameFromId(l))
            .ToHashSet();

        Dictionary<Deck, CharacterLocationsSummary> summary = [];

        foreach (var (deck, charName) in Archipelago.DeckToItem.Append(new KeyValuePair<Deck, string>(Deck.tooth, "Basic")))
        {
            summary[deck] = new CharacterLocationsSummary
            {
                Cards = ([], [], []),
                Artifacts = ([], [])
            };
            
            var deckLocations = allLocations.Where(l => l.Contains(charName)).ToList();
            var deckCheckedLocations = allCheckedLocations.Intersect(deckLocations).ToHashSet();
            
            deckLocations.ForEach(location =>
            {
                if (location.Contains("Common Card"))
                    summary[deck].Cards.Common[location] = deckCheckedLocations.Contains(location);
                else if (location.Contains("Uncommon Card"))
                    summary[deck].Cards.Uncommon[location] = deckCheckedLocations.Contains(location);
                else if (location.Contains("Rare Card"))
                    summary[deck].Cards.Rare[location] = deckCheckedLocations.Contains(location);
                else if (location.Contains("Boss Artifact"))
                    summary[deck].Artifacts.Boss[location] = deckCheckedLocations.Contains(location);
                else if (location.Contains("Artifact"))
                    summary[deck].Artifacts.Common[location] = deckCheckedLocations.Contains(location);
                else if (location.StartsWith("Fix"))
                    summary[deck].Memories[location] = deckCheckedLocations.Contains(location);
            });
            
            summary[deck].Cards.Common.ToList().ForEach(kvp => summary[deck].AllCards[kvp.Key] = kvp.Value);
            summary[deck].Cards.Uncommon.ToList().ForEach(kvp => summary[deck].AllCards[kvp.Key] = kvp.Value);
            summary[deck].Cards.Rare.ToList().ForEach(kvp => summary[deck].AllCards[kvp.Key] = kvp.Value);
            summary[deck].Artifacts.Common.ToList().ForEach(kvp => summary[deck].AllArtifacts[kvp.Key] = kvp.Value);
            summary[deck].Artifacts.Boss.ToList().ForEach(kvp => summary[deck].AllArtifacts[kvp.Key] = kvp.Value);
        }

        return summary;
    }
}
