using System.Reflection;
using DemoMod.Actions;
using Nanoray.PluginManager;
using Nickel;

namespace DemoMod.Artifacts;

/*
 * Artifacts are a nice way to accentuate a character's potential.
 * They can be simple effects that occur at simple times, they can modify an existing mechanic or one introduced by the character.
 * Similarly to cards, ensure you add this type to your ModEntry for registration.
 */
public class BuriedKnowledge : Artifact, IRegisterable
{
    public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
    {
        helper.Content.Artifacts.RegisterArtifact(new ArtifactConfiguration
        {
            ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
            Meta = new ArtifactMeta
            {
                pools = [ArtifactPool.Common],
                owner = ModEntry.Instance.DemoDeck.Deck
            },
            Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "BuriedKnowledge", "name"]).Localize,
            Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "BuriedKnowledge", "desc"]).Localize,
            /*
             * For Artifacts with just one sprite, registering them at the place of usage helps simplify things.
             */
            Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifact/Ashtray.png")).Sprite
        });
    }
    
    /*
     * Unlike Cards, Artifacts have no required methods. Implement the ones you need, and leave the rest unimplemented.
     * By default, Artifacts have everything implemented with methods that do nothing, so there is no need to call the super.
     */
    public override void OnCombatStart(State state, Combat combat)
    {
        combat.Queue(new AGainPonder
        {
            Count = 3,
            Destination = CardDestination.Discard,
            artifactPulse = Key() // This makes it so that when this action occurs, this artifact is pulsed with a white aura.
        });
    }
}