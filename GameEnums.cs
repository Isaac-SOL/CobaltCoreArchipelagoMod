using Nanoray.EnumByNameSourceGenerator;

namespace CobaltCoreArchipelago;

/*
 * Enumeration (enum) types in C# are internally stored as numbers.
 * This means that, if a future update changes the order of sprites, old sprite references will become result in unexpected behavior.
 * The EnumByName annotation allows you to create a "stable" version of these enumerations, ensuring you always get the value tied to what you want.
 */
[EnumByName(typeof(Spr))]
internal static partial class StableSpr { }

[EnumByName(typeof(UK))]
internal static partial class StableUK { }

internal enum ArchipelagoUK
{
    connection_host = 208001,
    connection_port,
    connection_slot,
    connection_password,
    connection_connect,
    connection_back,
    connection_finalizeConnection,
    connection_seePassword
}

internal static class ArchipelagoUKExtensions
{
    public static UK ToUK(this ArchipelagoUK auk) => (UK)auk;
}