using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HarmonyLib;
using Microsoft.Extensions.Logging;

namespace CobaltCoreArchipelago.StoryPatches;

[HarmonyPatch(typeof(RunWinHelpers), nameof(RunWinHelpers.GetChoices))]
[HarmonyPriority(Priority.Low)]
public class RunWinWhoPatch
{
    public static void Postfix(ref List<Choice> __result, State s)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        var decksToAdd = s.characters
            .Where(ch =>
                       ch.deckType is { } deck
                       && GetMemoryCountForChoiceDisplay(deck, s) < 3
                       && (Archipelago.InstanceSlotData.AddCharacterMemories
                           || deck is not (Deck.colorless or Deck.shard)))
            .Select(ch => ch.deckType!.Value)
            .ToList();
        __result = decksToAdd
            .Select(deck =>
            {
                var memCount = GetMemoryCountForChoiceDisplay(deck, s);
                return new Choice
                {
                    label = $"<c={deck.Key()}>{Character.GetDisplayName(deck, s).ToUpperInvariant()}</c>."
                            + $" ({memCount}/3)", // Show current memory counts for each character
                    key = ".runWin_" + deck.Key(),
                    actions = { new ARunWinCharChoice { deck = deck } }
                };
            })
            .ToList();
        
        // Add the choice to get all memories
        if (!Archipelago.InstanceSlotData.UnlockMemoryForAllCharacters || decksToAdd.Count <= 1) return;
        
        __result.Add(new Choice
        {
            label = ModEntry.Instance.Localizations.Localize(["story", "memory", decksToAdd.Count == 2 ? "twoChoice" : "allChoice"]),
            key = ".runWin_AllOfThem",
            actions = decksToAdd
                .Select(deck => new ARunWinCharChoice { deck = deck })
                .Cast<CardAction>()
                .ToList()
        });
    }

    public static int GetMemoryCountForChoiceDisplay(Deck deck, State s)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        var count = Archipelago.InstanceSlotData.ShuffleMemories
            ? Archipelago.Instance.APSaveData.GetFixTimelineAmountIfShuffled(deck)
            : Archipelago.Instance.APSaveData.GetFixTimelineAmountIfNotShuffled(deck, s);
        ModEntry.Instance.Logger.LogInformation("RunWinWho memory count {char}: {n}",
                                                Character.GetDisplayName(deck, s), count);
        return count;
    }
}
