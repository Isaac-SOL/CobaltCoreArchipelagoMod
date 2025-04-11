# Chapter ?? - nickel.json

The nickel.json file describes essential information of your mod to the modloader, Nickel, to ensure it loads properly, as well as some metadata.
It can be thought of as a manifest of the mod's contents.

The following is an exhaustive list of a nickel.json's fields, as of Nickel 1.5.7:
* UniqueName - `string` - Required - The mod's unique name. It is internally prepended to things registered by this mod.
* Version - `SemanticVersion` - Required - The mod's version.
* RequiredApiVersion - `SemanticVersion` - Required - The version of Nickel this mod requires.
* Dependencies - `IReadOnlySet<ModDependency>` - The list of this mod's dependencies. Covered in detail later.
* DisplayName - `string` - The name used for front-facing purposes.
* Description - `string` - A summary of the mod's contents.
* Author - `string` - Who created this mod.
* ModType - `string` - The type of mod. Should probably be omitted.
* LoadPhase - `string` - The phase at which the mod loads. Covered in detail later.
* Submods - `IReadOnlyList<ISubmodEntry>` - The submods of this mod.

## Semantic Versioning

[Semantic Versioning](https://semver.org/) is a system describing how something's versions should proceed.
It is advised you follow it.

## Dependencies

Mod Dependencies declare what mods should be loaded before this mod, as well if this mod can even load without them.

The following is an exhaustive list of their fields:
* UniqueName - `string` - Required - The dependency's unique name.
* SemanticVersion - `SemanticVersion` - The required version of the dependency.
* IsRequired - `bool` - defaults to `true` - If this dependency is required for this mod to load.

## LoadPhase

Mods can load at various phases of the game's initialization, for various purposes.

`BeforeGameAssembly` loads the mod before the game's assembly.
This is helpful if you wish to use Mono.Cecil to modify the game's assembly before it loads.

`AfterGameAssembly` loads the mod after the game's assembly, but before the database has been initialized.
This is the default load phase.

`AfterDbInit` loads the mod after the game's database has been initialized, right before the game is launched.

## Submods

Mods can define submods, for example if a mod must leverage multiple LoadPhases.

The following is an exhaustive list of their fields:
* Manifest - `IModManifest` - Required - The manifest of the mod. Refer back to the format declared at the top.
* IsOptional - `bool` - defaults to `true`