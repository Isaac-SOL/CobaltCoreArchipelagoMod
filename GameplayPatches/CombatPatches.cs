using System.Diagnostics;
using HarmonyLib;

namespace CobaltCoreArchipelago.GameplayPatches;

public class CombatPatches;

// Check that cards are locked or unlocked each time they are drawn
// This is *a tad* overkill, but shouldn't have much of a performance impact,
// and ensures that the cards are always consistent with the AP inventory
[HarmonyPatch(typeof(Combat), nameof(Combat.SendCardToHand))]
public static class SendCardToHandPatch
{
    public static void Postfix(Card card)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        if (!Archipelago.CardToItem.ContainsKey(card.GetType())) return;
        if (Archipelago.Instance.APSaveData.HasCard(card.GetType()))
        {
            card.unplayableOverride = false;
            card.unplayableOverrideIsPermanent = false;
        }
        else
        {
            card.unplayableOverride = true;
            card.unplayableOverrideIsPermanent = true;
        }
    }
}