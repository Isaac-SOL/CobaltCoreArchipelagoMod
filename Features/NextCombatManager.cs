using System.Collections.Generic;

namespace CobaltCoreArchipelago.Features;

public static class NextCombatManager
{
    private static List<CardAction> nextCombatStartActions = [];
    
    internal static void Queue(CardAction action)
    {
        nextCombatStartActions.Add(action);
    }

    internal static bool HasCombatStartActions() => nextCombatStartActions.Count > 0;

    internal static CardAction Dequeue()
    {
        var nextAction = nextCombatStartActions[0];
        nextCombatStartActions.RemoveAt(0);
        return nextAction;
    }
}