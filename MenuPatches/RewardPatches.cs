using System.Diagnostics;
using System.Linq;
using CobaltCoreArchipelago.Artifacts;
using CobaltCoreArchipelago.Cards;
using HarmonyLib;
using Microsoft.Extensions.Logging;

namespace CobaltCoreArchipelago.MenuPatches;

public class RewardPatches;

[HarmonyPatch(typeof(CardReward), nameof(CardReward.Render))]
public static class CardRewardRenderPatch
{
    private static double rescoutTimer = 0.0;
    
    public static void Prefix(CardReward __instance)
    {
        // Reset the time when we have a new CardReward screen
        // This is important because we don't want to overlap the first scouting done by CardOfferingPatch
        if (!__instance.didFirstFrame)
            rescoutTimer = 0.0;
    }

    public static void Postfix(CardReward __instance, G g)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        
        if (Archipelago.Instance.APSaveData.CardScoutMode == CardScoutMode.DontScout) return;

        // Recheck not scouted AP checks every 5 seconds
        if (rescoutTimer > 5.0)
        {
            rescoutTimer -= 5.0;

            var checkCards = __instance.cards
                .Where(card => card is CheckLocationCard)
                .Cast<CheckLocationCard>().ToList();
            var locations = checkCards.Select(card => card.locationName).ToArray();

            if (locations.Length > 0)
            {
                Archipelago.Instance.ScoutLocationInfo(locations).ContinueWith(task =>
                {
                    for (var i = 0; i < checkCards.Count; i++)
                        checkCards[i].LoadInfo(task.Result[i]);
                });
            }
        }
        rescoutTimer += g.dt;
    }
}

[HarmonyPatch(typeof(ArtifactReward), nameof(ArtifactReward.Render))]
public static class ArtifactRewardRenderPatch
{
    private static double rescoutTimer = 0.0;

    public static void Prefix(ArtifactReward __instance)
    {
        // Reset the time when we have a new ArtifactReward screen
        // This is important because we don't want to overlap the first scouting done by ArtifactOfferingPatch
        if (!__instance.playedIntroSound)
            rescoutTimer = 0.0;
    }

    public static void Postfix(ArtifactReward __instance, G g)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        
        if (Archipelago.Instance.APSaveData.CardScoutMode == CardScoutMode.DontScout) return;
        
        // Recheck not scouted AP checks every 5 seconds
        if (rescoutTimer > 5.0)
        {
            rescoutTimer -= 5.0;
        
            var checkArtifacts = __instance.artifacts
                .Where(artifact => artifact is CheckLocationArtifact)
                .Cast<CheckLocationArtifact>().ToList();
            var locations = checkArtifacts.Select(artifact => artifact.locationName).ToArray();

            if (locations.Length > 0)
            {
                Archipelago.Instance.ScoutLocationInfo(locations).ContinueWith(task =>
                {
                    for (var i = 0; i < checkArtifacts.Count; i++)
                    {
                        var info = task.Result[i];
                        if (info is null)
                            checkArtifacts[i].SetTextInfo("[]", "[]", APColors.Trap);
                        else
                            checkArtifacts[i].SetTextInfo(info.ItemName, info.Player.Name, info.GetColor());
                    }
                });
            }
        }
        rescoutTimer += g.dt;
    }
}

