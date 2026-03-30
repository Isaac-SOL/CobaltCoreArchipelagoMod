using daisyowl.text;

namespace CobaltCoreArchipelago.Features;

internal class APShout
{
    internal required string message;
    internal double progress;
    internal double delay;
    internal double lastBabble = -4.0;
    internal const double LETTERS_PER_SECOND = 50.0;
    internal const double TIMEOUT_AFTER_DONE_PRINTING = 4.0;
    internal const double BABBLE_INTERVAL_LETTERS = 4.0;

    public bool IsSilentLine() => GetTextWithoutTags() is "..." or "...!" or "...?" or "???";

    public bool IsDonePrinting() => progress > GetTextWithoutTags().Length;

    public void SkipToEnd()
    {
        var length = GetTextWithoutTags().Length;
        if (progress < length) progress = length;
    }

    public string GetTextWithoutTags() => TextParser.GetTextWithoutTags(message);

    public void Update(G g)
    {
        delay -= g.dt;
        if (delay <= 0.0)
            delay = 0.0;
        if (delay != 0.0)
            return;
        progress += g.dt * LETTERS_PER_SECOND;
        if (!(progress > GetTextWithoutTags().Length) && !(progress <= lastBabble + BABBLE_INTERVAL_LETTERS))
        {
            lastBabble = progress;
            if (!IsSilentLine())
                Audio.Play(FSPRO.Event.Babble_default);
        }
    }
}