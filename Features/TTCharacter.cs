namespace CobaltCoreArchipelago.Features;

public class TTCharacter : Tooltip
{
    public required Character character;

    public override Rect Render(G g, bool dontDraw)
    {
        if (!dontDraw)
        {
            character.Render(
                g,
                0, 2,
                mini: true,
                showTooltips: false,
                canFocus: false
            );
        }
        return new Rect(0.0, 2.0, 35.0, 33.0);
    }
}