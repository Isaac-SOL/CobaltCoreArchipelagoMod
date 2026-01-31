using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using CobaltCoreArchipelago.Actions;
using CobaltCoreArchipelago.GameplayPatches;

namespace CobaltCoreArchipelago.Features;

public class DeathLinkManager
{
    public static bool PreventDeathLink { get; set; } = false;

    internal static void ApplyDeathLink(G g, DeathLink lastDeathLink)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        var state = g.state;
        var combat = state.route as Combat;
        switch (Archipelago.Instance.APSaveData.DeathLinkMode)
        {
            case DeathLinkMode.Missing:
                // Just give the status effect. No risk of dying in this mode
                if (combat is not null)
                {
                    combat.Queue([
                        new AStatus
                        {
                            status = GetAssignableStatuses(state).Random(state.rngActions),
                            statusAmount = 1,
                            targetPlayer = true
                        },
                        new AInvalidateUndos
                        {
                            type = InvalidationTypes.DeathlinkReceived
                        }
                    ]);
                }
                break;
            case DeathLinkMode.HullDamage:
                // Simulate if the action will kill the player.
                // If it does, perform like death mode.
                // Otherwise, damage them through an action
                var fakeState = Mutil.DeepCopy(state);
                fakeState.ship.DirectHullDamage(state, DB.fakeCombat, Archipelago.Instance.APSaveData.DeathLinkHullDamage);
                if (fakeState.ship.hull == 0)
                {
                    state.ship.hull = 0;
                    SnapScreen(g, state, combat);
                    FinishApplyFullDeathLink(lastDeathLink);
                }
                else if (combat is not null)
                {
                    combat.Queue([
                        new AHurt
                        {
                            hurtAmount = Archipelago.Instance.APSaveData.DeathLinkHullDamage,
                            targetPlayer = false,
                            hurtShieldsFirst = false,
                            cannotKillYou = true
                        },
                        new AInvalidateUndos
                        {
                            type = InvalidationTypes.DeathlinkReceived
                        }
                    ]);
                }
                else
                {
                    new AHurt
                    {
                        hurtAmount = Archipelago.Instance.APSaveData.DeathLinkHullDamage,
                        targetPlayer = false,
                        hurtShieldsFirst = false,
                        cannotKillYou = true
                    }.Begin(g, state, DB.fakeCombat);
                }
                break;
            case DeathLinkMode.Death:
                // Kill the player
                state.ship.hull = 0;
                SnapScreen(g, state, combat);
                FinishApplyFullDeathLink(lastDeathLink);
                break;
        }
                
        // Add to the count and save
        Archipelago.Instance.APSaveData.DeathLinkCount++;
        APSaveData.Save();
    }
    
    private static void FinishApplyFullDeathLink(DeathLink lastDeathLink)
    {
        // Save a message to replace the void shout
        GetVoidShoutPatch.DeathLinkMessage = lastDeathLink!.Cause is null 
            ? $"{lastDeathLink.Source}?"
            : $"{lastDeathLink.Source}\n{lastDeathLink.Cause}";
        // Ensures that this received DeathLink won't cause us to trigger a new DeathLink ourselves
        PreventDeathLink = true;
    }

    private static void SnapScreen(G g, State state, Combat? combat)
    {
        // Snaps us to the base screen to trigger the animation right now.
        state.routeOverride = null;
        if (combat is not null)
            combat.routeOverride = null;
        if (g.metaRoute is not null)
            g.CloseRoute(g.metaRoute);
    }
    
    internal static List<Status> GetAssignableStatuses(State s)
    {
        return s.characters.Select(c =>
        {
            var deckType = c.deckType;
            if (!deckType.HasValue) return Status.heat;
            return deckType.GetValueOrDefault() switch
            {
                Deck.colorless => Status.missingCat,
                Deck.dizzy => Status.missingDizzy,
                Deck.riggs => Status.missingRiggs,
                Deck.peri => Status.missingPeri,
                Deck.goat => Status.missingIsaac,
                Deck.eunice => Status.missingDrake,
                Deck.hacker => Status.missingMax,
                Deck.shard => Status.missingBooks,
                _ => Status.heat
            };
        }).ToList();
    }
}