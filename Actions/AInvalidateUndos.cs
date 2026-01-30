using TheJazMaster.CombatQoL;

namespace CobaltCoreArchipelago.Actions;

public class AInvalidateUndos : CardAction
{
    public AInvalidateUndos()
    {
        timer = 0.0;
    }
    
    public override void Begin(G g, State s, Combat c)
    {
        var combatQoL = ModEntry.Instance.CombatQol;
        combatQoL?.InvalidateUndos(c, ICombatQolApi.InvalidationReason.CUSTOM_REASON,
                                   ModEntry.Instance.Localizations.Localize(
                                       ["action", "AInvalidateUndos", "cannotUndoReason"]));
    }
}