using System.Collections.Generic;
using System.Linq;
using TheJazMaster.CombatQoL;

namespace CobaltCoreArchipelago.Actions;

public class AInvalidateUndos : CardAction
{
    public InvalidationTypes type = InvalidationTypes.ItemReceived;
    
    public AInvalidateUndos()
    {
        timer = 0.0;
    }

    private string Localize(params string[] key) => ModEntry.Instance.Localizations
        .Localize(new List<string>{"action", "AInvalidateUndos"}.Concat(key).ToArray());
    
    public override void Begin(G g, State s, Combat c)
    {
        var combatQoL = ModEntry.Instance.CombatQol;
        combatQoL?.InvalidateUndos(c, ICombatQolApi.InvalidationReason.CUSTOM_REASON,
                                   type switch
                                   {
                                       InvalidationTypes.DeathlinkReceived => Localize("reasonDeathlinkReceived"),
                                       _ => Localize("reasonItemReceived")
                                   });
    }
}

public enum InvalidationTypes
{
    ItemReceived,
    DeathlinkReceived
}