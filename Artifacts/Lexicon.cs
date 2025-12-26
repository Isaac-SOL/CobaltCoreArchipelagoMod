using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace CobaltCoreArchipelago.Artifacts;

/*
 * Some Artifacts do not use any of the many hooks available. It may not even utilize the one for setting its sprite.
 * In these cases, they are instead checked at the site of usage.
 * Lexicon is used in AGainPonder.GetNextUpgrade
 */
public class Lexicon : Artifact, IRegisterable
{
    private static Spr _sprA;
    private static Spr _sprB;

    public Upgrade Upgrade = Upgrade.A;
    
    public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
    {
        _sprA = helper.Content.Sprites
            .RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifact/LexiconA.png")).Sprite;
        _sprB = helper.Content.Sprites
            .RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifact/LexiconB.png")).Sprite;
        
        helper.Content.Artifacts.RegisterArtifact(new ArtifactConfiguration
        {
            ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
            Meta = new ArtifactMeta
            {
                pools = [ArtifactPool.Boss],
                owner = ModEntry.Instance.DemoDeck.Deck
            },
            Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Lexicon", "name"]).Localize,
            Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Lexicon", "desc"]).Localize,
            /*
             * For Artifacts with multiple sprites, a sprite must still be defined.
             */
            Sprite = _sprA
        });
    }

    public Upgrade PullAndFlip()
    {
        var up = Upgrade;
        Upgrade = Upgrade switch
        {
            Upgrade.A => Upgrade.B,
            _ => Upgrade.A
        };
        return up;
    }

    /*
     * An Artifact can be made to switch between multiple sprites.
     * This is called every frame, so it shouldn't be an expensive function.
     */
    public override Spr GetSprite() => Upgrade switch
    {
        Upgrade.B => _sprB,
        _ => _sprA
    };
}