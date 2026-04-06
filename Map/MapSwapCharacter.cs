using System.Collections.Generic;
using CobaltCoreArchipelago.Actions;

namespace CobaltCoreArchipelago.Map;

public class MapSwapCharacter : MapNodeContents
{
    public override void Render(G g, Vec v)
    {
        Draw.Sprite(AArchipelagoCheckLocation.Spr, v.x, v.y);
    }

    public override List<Tooltip> GetTooltips(G g)
    {
        return [new TTText("Faut swap des persos la")];
    }

    public override Route MakeRoute(State s, Vec coord)
    {
        throw new System.NotImplementedException();
    }
}