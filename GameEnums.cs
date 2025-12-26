using Nanoray.EnumByNameSourceGenerator;

namespace CobaltCoreArchipelago;

/*
 * Enumeration (enum) types in C# are internally stored as numbers.
 * This means that, if a future update changes the order of sprites, old sprite references will become result in unexpected behavior.
 * The EnumByName annotation allows you to create a "stable" version of these enumerations, ensuring you always get the value tied to what you want.
 */
[EnumByName(typeof(Spr))]
internal static partial class StableSpr { }