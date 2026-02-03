using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CobaltCoreArchipelago.Actions;
using CobaltCoreArchipelago.Features;
using CobaltCoreArchipelago.StoryPatches;
using HarmonyLib;
using Microsoft.Extensions.Logging;

namespace CobaltCoreArchipelago;

public static class ItemApplier
{
    private static List<(string name, string sender)> DeferredUnappliedItems { get; set; } = [];
    
    internal static bool CanApplyItems => Archipelago.Instance.Ready;
    
    internal static void ApplyReceivedItem((string name, string sender) item, State? state = null)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        if (state is null || !CanApplyItems)
        {
            DeferredUnappliedItems.Add(item);
            return;
        }

        var slotData = Archipelago.InstanceSlotData;
        var combat = state.route as Combat;
        
        // If we have CombatQoL installed, any state update can be undone in combat unless we explicitly prevent it
        // We add a guard both *before* and *after* just in case the player undoes at the wrong time
        combat?.Queue(new AInvalidateUndos());
        
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
            if (Archipelago.InstanceSlotData.ShuffleMemories)
            {
                // Memories are shuffled: we get the values from the AP inventory and ignore the Cobalt Core value
                var count = Archipelago.Instance.APSaveData.AppliedInventory.TryGetValue(item.name, out var currCount)
                    ? currCount + 1
                    : 1;
                UnlockReplacements.SetMemoryCount(state, deckMemory, count);
            }
            else
            {
                ModEntry.Instance.Logger.LogError("Received {memory}, but memories aren't shuffled. Bug?", item.name);
            }
        }
        else if (Archipelago.ItemToCard.TryGetValue(item.name, out var card))
        {
            if (state.storyVars.hasStartedGame)
            {
                var newCard = (Card)card.CreateInstance();
                if (slotData.HasImmediateCardAttribute(CardRewardAttribute.Temporary))
                    newCard.temporaryOverride = true;
                if (slotData.HasImmediateCardAttribute(CardRewardAttribute.SingleUse))
                    newCard.singleUseOverride = true;
                if (slotData.HasImmediateCardAttribute(CardRewardAttribute.Exhaust))
                {
                    newCard.exhaustOverride = true;
                    newCard.exhaustOverrideIsPermanent = true;
                }
                if (slotData.HasImmediateCardAttribute(CardRewardAttribute.Discount))
                    newCard.discount = -1;
                if (slotData.HasImmediateCardAttribute(CardRewardAttribute.Recycle))
                {
                    newCard.recycleOverride = true;
                    newCard.recycleOverrideIsPermanent = true;
                }
                if (slotData.HasImmediateCardAttribute(CardRewardAttribute.Retain))
                {
                    newCard.retainOverride = true;
                    newCard.recycleOverrideIsPermanent = true;
                }
                var newCardMeta = newCard.GetMeta();
                var local = item.sender == Archipelago.Instance.APSaveData.Slot;
                var hasDeck = state.characters.Any(character => character.deckType == newCardMeta.deck);
                if (slotData.ImmediateCardRewards switch
                    {
                        CardRewardsMode.IfLocal => local,
                        CardRewardsMode.IfHasDeck => hasDeck,
                        CardRewardsMode.IfLocalAndHasDeck => local && hasDeck,
                        CardRewardsMode.Always => true,
                        _ => false
                    })
                {
                    if (combat is not null && !combat.EitherShipIsDead(state))
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
                if (slotData.ImmediateArtifactRewards switch
                    {
                        CardRewardsMode.IfLocal => local,
                        CardRewardsMode.IfHasDeck => hasDeck,
                        CardRewardsMode.IfLocalAndHasDeck => local && hasDeck,
                        CardRewardsMode.Always => true,
                        _ => false
                    } && !ArtifactReward.GetBlockedArtifacts(state).Contains(artifact))
                {
                    if (combat is not null && !combat.EitherShipIsDead(state))
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
        else if (item.name == "1 Energy")
        {
            if (combat is not null)
            {
                combat.Queue(new AEnergy
                {
                    changeAmount = 1
                });
            }
            // TODO do it on next combat otherwise
        }
        else if (item.name == "3 Temp Shield")
        {
            if (combat is not null)
            {
                combat.Queue(new AStatus
                {
                    status = Status.tempShield,
                    statusAmount = 3,
                    targetPlayer = true,
                    mode = AStatusMode.Add
                });
            }
            else
            {
                state.ship.Add(Status.tempShield, 3);
            }
        }
        else if (item.name == "Missing Trap!")
        {
            if (combat is not null)
            {
                combat.Queue(new AStatus
                {
                    status = DeathLinkManager.GetAssignableStatuses(state).Random(state.rngActions),
                    statusAmount = 1,
                    targetPlayer = true
                });
            }
        }
        
        // If we have CombatQoL installed, any state update can be undone in combat unless we explicitly prevent it
        combat?.Queue(new AInvalidateUndos());
        Archipelago.Instance.APSaveData.AddAppliedItem(item.name);
    }

    internal static void ApplyDeferredItems(State state)
    {
        if (!CanApplyItems)
            ModEntry.Instance.Logger.LogWarning("Trying to apply deferred items while unable to apply them");
        var toApply = DeferredUnappliedItems;
        DeferredUnappliedItems = [];
        foreach (var item in toApply)
        {
            ApplyReceivedItem(item, state);
        }
    }
}