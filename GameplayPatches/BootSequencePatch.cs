using System;
using System.Collections.Generic;
using System.Diagnostics;
using CobaltCoreArchipelago.Actions;
using HarmonyLib;

namespace CobaltCoreArchipelago.GameplayPatches;

[HarmonyPatch(typeof(Events), nameof(Events.BootSequence))]
public class BootSequencePatch
{
    private const int MaxArtifactChoices = 4;
    
    public static void Postfix(List<Choice> __result, State s)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");

        List<Choice> possibleChoices = [];

        if (CardBrowseListPatch.GetPickableUnlockedCardsList(s).Count > 0)
        {
            possibleChoices.Add(new Choice
            {
                label = ModEntry.Instance.Localizations.Localize(["cardBrowse", "bootOptionUnlockedCardName"]),
                key = ".zone_first",
                actions =
                [
                    new AAPCardSelect
                    {
                        browseAction = new CardSelectAdd(),
                        browseSource = CardBrowse.Source.Deck,
                        browseData = new CardBrowseAPData
                        {
                            filterMode = CardBrowseAPData.FilterMode.UnlockedCardsNotInDeck
                        },
                        allowCancel = true
                    }
                ]
            });
        }

        var pickableArtifactsCount = Math.Min(CardBrowseListPatch.GetPickableUnlockedArtifactsList(s).Count, MaxArtifactChoices);
        if (pickableArtifactsCount > 0 && false)
        {
            possibleChoices.Add(new Choice
            {
                label = string.Format(ModEntry.Instance.Localizations.Localize(["cardBrowse", "bootOptionUnlockedArtifactName"]), pickableArtifactsCount),
                key = ".zone_first",
                actions =
                [
                    new AAPArtifactOffering
                    {
                        amount = MaxArtifactChoices,
                        data = new ArtifactOfferingAPData
                        {
                            filterMode = ArtifactOfferingAPData.FilterMode.UnlockedArtifactsNotInDeck
                        }
                    }
                ]
            });
        }
        
        if (possibleChoices.Count > 0)
            __result.Add(possibleChoices.Random(s.rngCurrentEvent));
    }
}