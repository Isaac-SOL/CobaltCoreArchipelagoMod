using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CobaltCoreArchipelago.Actions;
using CobaltCoreArchipelago.StoryPatches;

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
                key = "saltyisaac_archipelago_SwapCharacter_Choice2",
                actions = [ new ASwapCharacterInChoice { deck = d } ]
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
        if (s.characters.Count < 3)
            return
            [
                new Choice
                {
                    label = Localize("optionLabelNoSwapOut"),
                    actions = [ new AAddCharacter { deck = swapIn }, new CrystalizeFriendBgPoof() ]
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
