using Microsoft.Extensions.Logging;

namespace CobaltCoreArchipelago;

internal static class Util
{
    internal static ILogger Log => ModEntry.Instance.Logger;
}