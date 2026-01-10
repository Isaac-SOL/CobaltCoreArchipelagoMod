using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using Nickel.ModSettings;
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
    internal IModSettingsApi ModSettings;
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
        ModSettings = helper.ModRegistry.GetApi<IModSettingsApi>("Nickel.ModSettings")!;
        Harmony = new Harmony("SaltyIsaac.CobaltCoreArchipelago");
        Harmony.PatchAll(Assembly.GetExecutingAssembly());
        Archipelago = new Archipelago();
        
        // Fill out static data
        BaseShips = Mutil.DeepCopy(StarterShip.ships);
        BaseDifficulties = Mutil.DeepCopy(NewRunOptions.difficulties);

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
        MkSlotPatch.ArchipelagoSaveSpr = RegisterSprite(package, "assets/UI/ArchipelagoSave.png").Sprite;
        MkSlotPatch.NotArchipelagoSaveSpr = RegisterSprite(package, "assets/UI/NotArchipelagoSave2.png").Sprite;

        ModSettings.RegisterModSettings(
            ModSettings.MakeList([
                ModSettings.MakeProfileSelector(
                    () => package.Manifest.DisplayName ?? package.Manifest.UniqueName,
                    // This is an attempt at making a dummy because I have no idea what is going on here
                    ProfileBasedValue.Create(
                        () => IModSettingsApi.ProfileMode.Slot,
                        _ => {},
                        _ => new List<int>(),
                        (_, _) => {}
                    )
                ),
                ModSettings.MakePadding(
                    ModSettings.MakeText(() => LocalizeSettings("profileWarning")),
                    4, 8),
                ModSettings.MakeCheckbox(
                        () => LocalizeSettings("deathlink", "settingName"),
                        () => Archipelago.APSaveData!.DeathLinkActive,
                        (_, _, value) => Archipelago.APSaveData!.DeathLinkActive = value)
                    .SetTooltips(() => [
                        new TTText(LocalizeSettings("deathlink", "tooltipName")),
                        new TTText(LocalizeSettings("deathlink", "desc"))
                    ]),
                ModSettings.MakeEnumStepper(
                        () => LocalizeSettings("automaticScouting", "settingName"),
                        () => Archipelago.APSaveData!.CardScoutMode,
                        value => Archipelago.APSaveData!.CardScoutMode = value)
                    .SetValueFormatter(value => value switch
                    {
                        CardScoutMode.DontScout => LocalizeSettings("automaticScouting", "nameDontScout"),
                        CardScoutMode.ScoutOnly => LocalizeSettings("automaticScouting", "nameScoutOnly"),
                        _ => LocalizeSettings("automaticScouting", "nameCreateHint")
                    })
                    .SetValueWidth(_ => 105)
                    .SetTooltips(() => new List<Tooltip>
                    {
                        new TTText(LocalizeSettings("automaticScouting", "tooltipName")),
                        new TTText(LocalizeSettings("automaticScouting", "desc")),
                        new TTDivider(),
                    }.Append(Archipelago.APSaveData!.CardScoutMode switch
                    {
                        CardScoutMode.DontScout => new TTText(LocalizeSettings("automaticScouting", "descDontScout")),
                        CardScoutMode.ScoutOnly => new TTText(LocalizeSettings("automaticScouting", "descScoutOnly")),
                        _ => new TTText(LocalizeSettings("automaticScouting", "descCreateHint"))
                    })),
                ModSettings.MakeCheckbox(
                        () => LocalizeSettings("bypassDifficulty", "settingName"),
                        () => Archipelago.APSaveData!.BypassDifficulty,
                        (_, _, value) => Archipelago.APSaveData!.BypassDifficulty = value)
                    .SetTooltips(() => [
                        new TTText(LocalizeSettings("bypassDifficulty", "tooltipName")),
                        new TTText(LocalizeSettings("bypassDifficulty", "desc"))
                    ])
            ]).SubscribeToOnMenuClose(_ =>
            {
                APSaveData.Save();
                if (Archipelago.APSaveData!.DeathLinkActive)
                    Archipelago.DeathLinkService!.EnableDeathLink();
                else
                    Archipelago.DeathLinkService!.DisableDeathLink();
                NewRunOptions.difficulties = Mutil.DeepCopy(BaseDifficulties);
                if (!Archipelago.APSaveData.BypassDifficulty && Archipelago.SlotDataHelper!.Value.MinimumDifficulty > 0)
                {
                    NewRunOptions.difficulties = NewRunOptions.difficulties.Where(difficulty =>
                                difficulty.level >= Archipelago.SlotDataHelper!.Value.MinimumDifficulty)
                        .ToList();
                }
            })
        );
    }

    private string LocalizeSettings(params string[] key) => Localizations.Localize(new List<string>{"settings"}.Concat(key).ToArray());
    
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

