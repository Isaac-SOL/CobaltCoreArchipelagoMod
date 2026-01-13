using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CobaltCoreArchipelago.StoryPatches;
using HarmonyLib;
using Microsoft.Extensions.Logging;

namespace CobaltCoreArchipelago;

public static class ItemApplier
{
    internal static List<(string name, string sender)> DeferredUnappliedItems { get; } = [];
    
    internal static bool CanApplyItems => Archipelago.Instance.Ready;
    
    internal static void ApplyReceivedItem((string name, string sender) item, State? state = null)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        if (state is null || !CanApplyItems)
        {
            DeferredUnappliedItems.Add(item);
            return;
        }
        
        if (Archipelago.ItemToStartingShip.TryGetValue(item.name, out var ship))
        {
            UnlockReplacements.UnlockShip(state, ship);
        }
        else if (Archipelago.ItemToDeck.TryGetValue(item.name, out var deck))
        {
            UnlockReplacements.UnlockChar(state, deck);
        }
        else if (Archipelago.ItemToMemory.TryGetValue(item.name, out var deckMemory))
        {
            UnlockReplacements.UnlockOneMemory(state.storyVars, deckMemory);
        }
        else if (Archipelago.ItemToCard.TryGetValue(item.name, out var card))
        {
            if (state.storyVars.hasStartedGame)
            {
                var newCard = (Card)card.CreateInstance();
                var newCardMeta = newCard.GetMeta();
                var local = item.sender == Archipelago.Instance.APSaveData.Slot;
                var hasDeck = state.characters.Any(character => character.deckType == newCardMeta.deck);
                if (Archipelago.InstanceSlotData.ImmediateCardRewards switch
                    {
                        CardRewardsMode.IfLocal => local,
                        CardRewardsMode.IfHasDeck => hasDeck,
                        CardRewardsMode.IfLocalAndHasDeck => local && hasDeck,
                        CardRewardsMode.Always => true,
                        _ => false
                    })
                {
                    if (state.route is Combat combat && !combat.EitherShipIsDead(state))
                    {
                        combat.Queue(new AAddCard
                        {
                            card = newCard,
                            destination = CardDestination.Hand
                        });
                    }
                    else
                    {
                        state.SendCardToDeck(newCard, doAnimation: true);
                    }
                }
            }
            // Also unlock cards in current deck if applicable
            UnlockReplacements.UnlockCodexCard(state, card);
        }
        else if (Archipelago.ItemToArtifact.TryGetValue(item.name, out var artifact))
        {
            if (state.storyVars.hasStartedGame)
            {
                var newArtifact = (Artifact)artifact.CreateInstance();
                var newArtifactMeta = newArtifact.GetMeta();
                var local = item.sender == Archipelago.Instance.APSaveData.Slot;
                var hasDeck = state.characters.Any(character => character.deckType == newArtifactMeta.owner);
                if (Archipelago.InstanceSlotData.ImmediateCardRewards switch
                    {
                        CardRewardsMode.IfLocal => local,
                        CardRewardsMode.IfHasDeck => hasDeck,
                        CardRewardsMode.IfLocalAndHasDeck => local && hasDeck,
                        CardRewardsMode.Always => true,
                        _ => false
                    })
                {
                    if (state.route is Combat combat && !combat.EitherShipIsDead(state))
                    {
                        combat.Queue(new AAddArtifact
                        {
                            artifact = newArtifact
                        });
                    }
                    else
                    {
                        state.SendArtifactToChar(newArtifact);
                    }
                }
            }
            // Also unlock artifacts in current deck if applicable
            UnlockReplacements.UnlockCodexArtifact(state, artifact);
        }

        Archipelago.Instance.APSaveData.AddAppliedItem(item.name);
    }

    internal static void ApplyDeferredItems(State state)
    {
        if (!CanApplyItems)
            ModEntry.Instance.Logger.LogWarning("Trying to apply deferred items while unable to apply them");
        var toApply = DeferredUnappliedItems;
        DeferredUnappliedItems.Clear();
        foreach (var item in toApply)
        {
            ApplyReceivedItem(item, state);
        }
    }
}