using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CobaltCoreArchipelago.Actions;
using CobaltCoreArchipelago.Artifacts;
using CobaltCoreArchipelago.Cards;
using CobaltCoreArchipelago.External;
using CobaltCoreArchipelago.ConnectionInfoMenu;
using CobaltCoreArchipelago.MenuPatches;

namespace CobaltCoreArchipelago;

internal class ModEntry : SimpleMod
{
    internal static ModEntry Instance { get; private set; } = null!;
    internal Harmony Harmony;
    internal IKokoroApi.IV2 KokoroApi;
    internal ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations { get; }
    internal ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations { get; }

    internal Archipelago Archipelago;
    internal static Dictionary<string, StarterShip> BaseShips { get; set; } = new();
    internal static List<NewRunOptions.DifficultyLevel> BaseDifficulties { get; set; } = [];

    internal IDeckEntry ArchipelagoDeck;
    internal IDeckEntry LockedDeck;
    
    /*
     * The following lists contain references to all types that will be registered to the game.
     * All cards and artifacts must be registered before they may be used in the game.
     * In theory only one collection could be used, containing all registrable types, but it is seperated this way for ease of organization.
     */
    private static List<Type> DemoCommonCardTypes = [
        typeof(CheckLocationCard)
    ];
    private static List<Type> DemoUncommonCardTypes = [
        typeof(CheckLocationCardUncommon)
    ];
    private static List<Type> DemoRareCardTypes = [
        typeof(CheckLocationCardRare)
    ];
    private static List<Type> DemoSpecialCardTypes = [
    ];
    private static IEnumerable<Type> DemoCardTypes =
        DemoCommonCardTypes
            .Concat(DemoUncommonCardTypes)
            .Concat(DemoRareCardTypes)
            .Concat(DemoSpecialCardTypes);

    private static List<Type> DemoCommonArtifacts = [
        typeof(CheckLocationArtifact),
        typeof(LockedArtifact)
    ];
    private static List<Type> DemoBossArtifacts = [
        typeof(CheckLocationArtifactBoss),
        typeof(LockedArtifactBoss)
    ];
    private static IEnumerable<Type> DemoArtifactTypes =
        DemoCommonArtifacts
            .Concat(DemoBossArtifacts);

    private static IEnumerable<Type> AllRegisterableTypes =
        DemoCardTypes
            .Concat(DemoArtifactTypes);

    public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
    {
        Instance = this;
        Harmony = new Harmony("SaltyIsaac.CobaltCoreArchipelago");
        Harmony.PatchAll(Assembly.GetExecutingAssembly());
        Archipelago = new Archipelago();
        
        // Fill out static data
        BaseShips = Mutil.DeepCopy(StarterShip.ships);
        BaseDifficulties = Mutil.DeepCopy(NewRunOptions.difficulties);
        
        /*
         * Some mods provide an API, which can be requested from the ModRegistry.
         * The following is an example of a required dependency - the code would have unexpected errors if Kokoro was not present.
         * Dependencies can (and should) be defined within the nickel.json file, to ensure proper load mod load order.
         */
        KokoroApi = helper.ModRegistry.GetApi<IKokoroApi>("Shockah.Kokoro")!.V2;

        AnyLocalizations = new JsonLocalizationProvider(
            tokenExtractor: new SimpleLocalizationTokenExtractor(),
            localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
        );
        Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
            new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(AnyLocalizations)
        );

        ArchipelagoDeck = helper.Content.Decks.RegisterDeck("Archipelago", new DeckConfiguration
        {
            Definition = new DeckDef
            {
                color = new Color("F763FF"),
                titleColor = new Color("000000")
            },
            DefaultCardArt = RegisterSprite(package, "assets/Card/ArchipelagoBack.png").Sprite,
            BorderSprite = RegisterSprite(package, "assets/frame_ap.png").Sprite,
            Name = AnyLocalizations.Bind(["deck", "name"]).Localize
        });

        LockedDeck = helper.Content.Decks.RegisterDeck("Locked", new DeckConfiguration
        {
            Definition = new DeckDef
            {
                color = new Color("333333"),
                titleColor = new Color("000000")
            },
            DefaultCardArt = StableSpr.cards_colorless,
            BorderSprite = StableSpr.cardShared_border_colorless,
            Name = AnyLocalizations.Bind(["deck", "lockedName"]).Localize
        });
        
        CheckLocationArtifact.BaseSpr = RegisterSprite(package, "assets/Artifact/Artifact_ap.png").Sprite;

        /*
         * All the IRegisterable types placed into the static lists at the start of the class are initialized here.
         * This snippet invokes all of them, allowing them to register themselves with the package and helper.
         */
        foreach (var type in AllRegisterableTypes)
            AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);

        AArchipelagoCheckLocation.Spr = RegisterSprite(package, "assets/ap_action.png").Sprite;

        CheckLocationCard.ArtCommon = RegisterSprite(package, "assets/Card/ArchipelagoBack2.png").Sprite;
        CheckLocationCard.ArtUncommon = RegisterSprite(package, "assets/Card/ArchipelagoBack5.png").Sprite;
        CheckLocationCard.ArtRare = RegisterSprite(package, "assets/Card/ArchipelagoBack7.png").Sprite;

        ConnectionInfoInput.TextBoxSpr = RegisterSprite(package, "assets/UI/Textbox.png").Sprite;
        ConnectionInfoInput.TextBoxHoverSpr = RegisterSprite(package, "assets/UI/Textbox_hover.png").Sprite;

        DrawCorePatch.SmolCobaltSpr = RegisterSprite(package, "assets/UI/SmolCobalt.png").Sprite;
        MainMenuRenderPatch.ArchipelagoTitleSpr = RegisterSprite(package, "assets/UI/ArchipelagoLogo.png").Sprite;
    }
    
    /*
     * assets must also be registered before they may be used.
     * Unlike cards and artifacts, however, they are very simple to register, and often do not need to be referenced in more than one place.
     * This utility method exists to easily register a sprite, but nothing prevents you from calling the method used yourself.
     */
    public static ISpriteEntry RegisterSprite(IPluginPackage<IModManifest> package, string dir)
    {
        return Instance.Helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile(dir));
    }
}

