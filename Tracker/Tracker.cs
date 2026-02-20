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

    internal int GetMaxScrollLength() => 150;

    public Tracker()
    {
        summaryCache = CharacterLocationsSummary.GetLocationsSummary();
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
        
        var basicCommonArtifactCount = summaryCache[Deck.tooth].Artifacts.Common.Count(kvp => kvp.Value);
        var basicBossArtifactCount = summaryCache[Deck.tooth].Artifacts.Boss.Count(kvp => kvp.Value);
        var basicArtifactCount = basicCommonArtifactCount + basicBossArtifactCount;
        
        var basicCommonArtifactAll = summaryCache[Deck.tooth].Artifacts.Common.Count;
        var basicBossArtifactAll = summaryCache[Deck.tooth].Artifacts.Boss.Count;
        var basicArtifactAll = basicCommonArtifactAll + basicBossArtifactAll;

        var basicArtifactsBox = g.Push(rect: new Rect(170, 42 + scroll, 100, 8),
                                  key: new UIKey(ArchipelagoUK.codex_charArtifacts.ToUK(), 0));
        Draw.Text(
            $"Basic Artifacts: {basicArtifactCount}/{basicArtifactAll}",
            basicArtifactsBox.rect.x, basicArtifactsBox.rect.y + 1,
            color: basicArtifactCount < basicArtifactAll ? Colors.white : CompletedColor
        );
        if (basicArtifactsBox.IsHover())
        {
            g.tooltips.AddText(basicArtifactsBox.rect.xy + new Vec(50, -2),
                               $"<c=artifact>BASIC ARTIFACTS</c>\n" +
                               $"Common: {basicCommonArtifactCount}/{basicCommonArtifactAll}\n" +
                               $"Boss: {basicBossArtifactCount}/{basicBossArtifactAll}");
        }
        g.Pop();

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

            var cardsBox = g.Push(rect: new Rect(170, 71 + offset, 100, 8),
                                  key: new UIKey(ArchipelagoUK.codex_charCards.ToUK(), ukOffset));
            Draw.Text(
                $"Cards: {cardCount}/{cardAll}",
                cardsBox.rect.x, cardsBox.rect.y + 1,
                color: cardCount < cardAll ? charColor : CompletedColor
            );
            if (cardsBox.IsHover())
            {
                g.tooltips.AddText(cardsBox.rect.xy + new Vec(50, -2),
                                   $"<c=artifact>{Character.GetDisplayName(deck, g.state).ToUpper()} CARDS</c>\n" +
                                   $"Common: {commonCardCount}/{commonCardAll}\n" +
                                   $"Uncommon: {uncommonCardCount}/{uncommonCardAll}\n" +
                                   $"Rare: {rareCardCount}/{rareCardAll}");
            }
            g.Pop();

            var artifactsBox = g.Push(rect: new Rect(170, 81 + offset, 100, 8),
                                      key: new UIKey(ArchipelagoUK.codex_charArtifacts.ToUK(), ukOffset));
            Draw.Text(
                $"Artifacts: {artifactCount}/{artifactAll}",
                artifactsBox.rect.x, artifactsBox.rect.y + 1,
                color: artifactCount < artifactAll ? charColor : CompletedColor
            );
            if (artifactsBox.IsHover())
            {
                g.tooltips.AddText(artifactsBox.rect.xy + new Vec(50, -2),
                                   $"<c=artifact>{Character.GetDisplayName(deck, g.state).ToUpper()} ARTIFACTS</c>\n" +
                                   $"Common: {commonArtifactCount}/{commonArtifactAll}\n" +
                                   $"Boss: {bossArtifactCount}/{bossArtifactAll}");
            }
            g.Pop();

            var memoriesBox = g.Push(rect: new Rect(170, 91 + offset, 100, 8),
                                     key: new UIKey(ArchipelagoUK.codex_charMemories.ToUK(), ukOffset));
            Draw.Text(
                $"Memory unlocks: {memoryCount}/{memoryAll}",
                memoriesBox.rect.x, memoriesBox.rect.y + 1,
                color: memoryCount < memoryAll ? charColor : CompletedColor
            );
            if (memoriesBox.IsHover())
            {
                g.tooltips.AddText(memoriesBox.rect.xy + new Vec(50, -2),
                                   $"<c=artifact>{Character.GetDisplayName(deck, g.state).ToUpper()} MEMORIES</c>\n" +
                                   $"All: {memoryCount}/{memoryAll}");
            }
            g.Pop();

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
    internal (Dictionary<string, bool> Common, Dictionary<string, bool> Boss) Artifacts;
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
                Artifacts = ([], []),
                Memories = []
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
        }

        return summary;
    }
}
