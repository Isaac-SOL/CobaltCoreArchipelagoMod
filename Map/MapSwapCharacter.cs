using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CobaltCoreArchipelago.Actions;
using CobaltCoreArchipelago.StoryPatches;
using HarmonyLib;

namespace CobaltCoreArchipelago.Map;

public class MapSwapCharacter : MapNodeContents
{
    public static Spr Spr;
    public static Deck swapIn;

    private static string Localize(params string[] key) => ModEntry.Instance.Localizations.Localize(
        new List<string> { "story", "event", "MapSwapCharacter" }.Concat(key).ToArray()
    );
    
    public override void Render(G g, Vec v)
    {
        Draw.Sprite(Spr, v.x - 1, v.y - 1);
    }

    public override List<Tooltip> GetTooltips(G g)
    {
        return [new TTText(Localize("tooltip"))];
    }

    public override Route MakeRoute(State s, Vec coord)
    {
        return Dialogue.MakeDialogueRouteOrSkip(
            s, DB.story.QuickLookup(s, s.characters.Count == 3
                                        ? "saltyisaac_archipelago_SwapCharacter"
                                        : "saltyisaac_archipelago_SwapCharacter_NoOut")
        );
    }

    public static List<Choice> ChooseCharToSwapIn(State s)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        return Archipelago.Instance.APSaveData.FoundChars
            .Where(d => s.characters.All(c => c.deckType != d))
            .Select(d => new Choice
            {
                label = $"Add <c={d.Key()}>{Character.GetDisplayName(d, s).ToUpperInvariant()}</c> to the crew."
                        + $" ({RunWinWhoPatch.GetMemoryCountForChoiceDisplay(d, s)}/3 mems)",
                key = s.characters.Count == 3
                    ? "saltyisaac_archipelago_SwapCharacter_Choice2"     // If we need to swap out
                    : $"saltyisaac_archipelago_SwapCharacter_{d.Key()}", // If we don't
                actions = s.characters.Count == 3
                    ? [new ASwapCharacterInChoice { deck = d }] : // If we need to swap out
                    [   // If we don't
                        new AAddCharacter { deck = d },
                        new AUpgradePartialCrewArtifact(),
                        new CrystalizeFriendBgPoof()
                    ]
            })
            .Append(new Choice
            {
                label = Loc.T("CrystallizedFriendEvent_Refuse", "They'll be fine."),
                key = "saltyisaac_archipelago_SwapCharacter_Refuse"
            })
            .ToList();
    }

    public static List<Choice> ChooseCharToSwapOut(State s)
    {
        // Fallback in case we have reached this node with less than 3 characters (this shouldn't happen)
        if (s.characters.Count < 3)
            return
            [
                new Choice
                {
                    label = Localize("optionLabelNoSwapOut"),
                    actions =
                    [
                        new AAddCharacter { deck = swapIn },
                        new AUpgradePartialCrewArtifact(),
                        new CrystalizeFriendBgPoof()
                    ]
                }
            ];

        return s.characters
            .Select(c => c.deckType!.Value)
            .Select(d => new Choice
            {
                label = $"Replace <c={d.Key()}>{Character.GetDisplayName(d, s).ToUpperInvariant()}</c> "
                        + $"with <c={swapIn.Key()}>{Character.GetDisplayName(swapIn, s).ToUpperInvariant()}</c> ",
                key = $"saltyisaac_archipelago_SwapCharacter_{swapIn.Key()}",
                actions =
                [
                    new ARemoveCharacter { deck = d },
                    new AAddCharacter { deck = swapIn },
                    new CrystalizeFriendBgPoof()
                ]
            })
            .Append(new Choice
            {
                label = Loc.T("ShopSkipConfirm_No", "On second thought..."),
                key = "saltyisaac_archipelago_SwapCharacter_ThinkAgain"
            })
            .ToList();
    }
}

internal class ASwapCharacterInChoice : CardAction
{
    public Deck deck;

    public override void Begin(G g, State s, Combat c)
    {
        MapSwapCharacter.swapIn = deck;
    }
}

internal class AUpgradePartialCrewArtifact : CardAction
{
    public override void Begin(G g, State s, Combat c)
    {
        if (ModEntry.Instance.CROAssembly is not { } cro) return;
        
        List<Type> partialCrewArtifacts =
        [
            AccessTools.GetTypesFromAssembly(cro)
                .First(type => type.Name == "PartialCrewRuns" && type.Namespace == "Shockah.CustomRunOptions")
                .GetNestedType("UnmannedRunArtifact", AccessTools.all)!,
            typeof(DailyJustOneCharacter),
            AccessTools.GetTypesFromAssembly(cro)
                .First(type => type.Name == "PartialCrewRuns" && type.Namespace == "Shockah.CustomRunOptions")
                .GetNestedType("DuoRunArtifact", AccessTools.all)!
        ];
        
        var crewCount = s.characters.Count;
        s.artifacts.RemoveAll(a => partialCrewArtifacts.Contains(a.GetType()));
        // We don't use a proper AAddArtifact to avoid triggering OnReceiveArtifact
        if (crewCount < 3)
            s.AddNonCharacterArtifact((Artifact)partialCrewArtifacts[crewCount].CreateInstance());
    }
}
