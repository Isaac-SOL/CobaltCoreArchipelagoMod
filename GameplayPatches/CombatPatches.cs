using System.Diagnostics;
using CobaltCoreArchipelago.Cards;
using CobaltCoreArchipelago.Features;
using HarmonyLib;

namespace CobaltCoreArchipelago.GameplayPatches;

public class CombatPatches;

[HarmonyPatch(typeof(Combat), nameof(Combat.SendCardToHand))]
public static class SendCardToHandPatch
{
    public static void Postfix(Card card)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        
        // Recheck non-scouted AP locations just in case
        if (Archipelago.Instance.APSaveData.CardScoutMode == CardScoutMode.DontScout
            || card is not CheckLocationCard apCard) return;
        
        if (apCard.locationItemName is null or "[]")
        {
            // TODO: this WILL break if we draw multiple non-scouted AP cards in quick succession
            Archipelago.Instance.ScoutLocationInfo(apCard.locationName).ContinueWith(task =>
            {
                apCard.LoadInfo(task.Result[0]);
            });
        }
    }
}

[HarmonyPatch(typeof(Combat), nameof(Combat.Make))]
public static class CombatStartPatch
{
    public static void Postfix(Combat __result)
    {
        while (NextCombatManager.HasCombatStartActions())
        {
            __result.Queue(NextCombatManager.Dequeue());
        }
    }
}
