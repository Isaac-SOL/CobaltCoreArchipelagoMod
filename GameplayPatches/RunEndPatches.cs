using System.Collections.Generic;
using System.Diagnostics;
using HarmonyLib;

namespace CobaltCoreArchipelago.GameplayPatches;


[HarmonyPatch(typeof(State), nameof(State.EndRun))]
public static class EndRunResetPatch
{
    static void Postfix(State __instance)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        if (Archipelago.InstanceSlotData.RandomizeStartingCards == FrequencyShuffleMode.EveryRun)
        {
            
        }
        
        if (Archipelago.InstanceSlotData.ShuffleShipParts == FrequencyShuffleMode.EveryRun)
        {
            // Patch starting ships
            foreach (var shipName in StarterShip.ships.Keys)
            {
                var shuffledParts = ModEntry.BaseShips[shipName].ship.parts
                    .Shuffle(Archipelago.Instance.APSaveData.ShipShuffleRand);
                StarterShip.ships[shipName].ship.parts = Mutil.DeepCopy(new List<Part>(shuffledParts));
            }
        }
        
        APSaveData.Save();
    }
}