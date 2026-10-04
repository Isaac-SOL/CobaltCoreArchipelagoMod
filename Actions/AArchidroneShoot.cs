using System.Collections.Generic;
using System.Linq;
using CobaltCoreArchipelago.Cards;

namespace CobaltCoreArchipelago.Actions;

public class AArchidroneShoot : CardAction
{
    public bool reverse;

    public override void Begin(G g, State s, Combat c)
    {
        timer = 0.0;
        var sortedList = new SortedList<int, CardAction>();
        foreach (var drone in c.stuff.Values.OfType<Archidrone>())
        {
            sortedList.Add(drone.x, new AAttack
            {
                fast = true,
                fromX = null,
                fromDroneX = drone.x,
                targetPlayer = drone.targetPlayer ^ reverse,
                damage = 1
            });
        }
        c.QueueImmediate(sortedList.Values);
    }
}