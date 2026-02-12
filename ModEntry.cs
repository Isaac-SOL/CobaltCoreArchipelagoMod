using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using Nickel.ModSettings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using CobaltCoreArchipelago.Actions;
using CobaltCoreArchipelago.Artifacts;
using CobaltCoreArchipelago.Cards;
using CobaltCoreArchipelago.ConnectionInfoMenu;
using CobaltCoreArchipelago.MenuPatches;
using CobaltCoreArchipelago.StoryPatches;
using TheJazMaster.CombatQoL;

namespace CobaltCoreArchipelago;

internal class ModEntry : SimpleMod
{
    internal static ModEntry Instance { get; private set; } = null!;
    internal Harmony Harmony;
    internal IModSettingsApi ModSettings;
    internal ICombatQolApi? CombatQol;
    internal ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations { get; }
    internal ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations { get; }

    internal Archipelago Archipelago;
    internal static Dictionary<string, StarterShip> BaseShips { get; set; } = new();
    internal static List<Deck> BaseCharsWithLore { get; set; } = [];

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
        typeof(CheckLocationCardRare),
        typeof(DeathLinkBoros)
    ];
    private static IEnumerable<Type> DemoCardTypes =
        DemoCommonCardTypes
            .Concat(DemoUncommonCardTypes)
            .Concat(DemoRareCardTypes);

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

    internal static IModCards ModCards => Instance.Helper.Content.Cards;

    public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
    {
        Instance = this;
        ModSettings = helper.ModRegistry.GetApi<IModSettingsApi>("Nickel.ModSettings")!;
        CombatQol = helper.ModRegistry.GetApi<ICombatQolApi>("TheJazMaster.CombatQoL");
        Harmony = new Harmony("SaltyIsaac.CobaltCoreArchipelago");
        Harmony.PatchAll(Assembly.GetExecutingAssembly());
        RunWinWhoPatch.ApplyPatch(Harmony);
        Archipelago = new Archipelago();
        
        // Fill out static data
        BaseShips = Mutil.DeepCopy(StarterShip.ships);
        BaseCharsWithLore = Mutil.DeepCopy(Vault.charsWithLore);

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

        AArchipelagoCheckLocation.Spr = RegisterSprite(package, "assets/ap_action.png").Sprite;

        CheckLocationCard.ArtCommon = RegisterSprite(package, "assets/Card/ArchipelagoBack2.png").Sprite;
        CheckLocationCard.ArtUncommon = RegisterSprite(package, "assets/Card/ArchipelagoBack5.png").Sprite;
        CheckLocationCard.ArtRare = RegisterSprite(package, "assets/Card/ArchipelagoBack7.png").Sprite;
        
        CheckLocationArtifact.BaseSpr = RegisterSprite(package, "assets/Artifact/Artifact_ap.png").Sprite;

        ConnectionInfoInput.TextBoxSpr = RegisterSprite(package, "assets/UI/Textbox.png").Sprite;
        ConnectionInfoInput.TextBoxHoverSpr = RegisterSprite(package, "assets/UI/Textbox_hover.png").Sprite;
        ConnectionInfoInput.LeftArrowSpr = RegisterSprite(package, "assets/UI/LeftArrow.png").Sprite;

        MainMenuPatch.TextBoxSpr = RegisterSprite(package, "assets/UI/Textbox_mainMenu.png").Sprite;
        MainMenuPatch.TextBoxHoverSpr = RegisterSprite(package, "assets/UI/Textbox_mainMenu_hover.png").Sprite;

        DrawCorePatch.SmolCobaltSpr = RegisterSprite(package, "assets/UI/SmolCobalt.png").Sprite;
        MainMenuPatch.ArchipelagoTitleSpr = RegisterSprite(package, "assets/UI/ArchipelagoLogo.png").Sprite;
        MkSlotPatch.ArchipelagoSaveSpr = RegisterSprite(package, "assets/UI/ArchipelagoSave.png").Sprite;
        MkSlotPatch.NotArchipelagoSaveSpr = RegisterSprite(package, "assets/UI/NotArchipelagoSave2.png").Sprite;
        
        BGRunWin.charFullBodySprites.Add(Deck.colorless, RegisterSprite(package, "assets/cat_end.png").Sprite);

        /*
         * All the IRegisterable types placed into the static lists at the start of the class are initialized here.
         * This snippet invokes all of them, allowing them to register themselves with the package and helper.
         */
        foreach (var type in AllRegisterableTypes)
            AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);
        
        // Add story memories immediately (we won't see them if charsWithLore is not patched anyway)
        AdditionalStoryNodes.Register(AdditionalStoryNodes.memoryNodes);

        ModSettings.RegisterModSettings(
            ModSettings.MakeList([
                ModSettings.MakeEnumStepper(
                        () => LocalizeSettings("deathlink", "settingName"),
                        () => Archipelago.APSaveData!.DeathLinkMode,
                        value => Archipelago.APSaveData!.DeathLinkMode = value)
                    .SetValueFormatter(value => value switch
                    {
                        DeathLinkMode.Off => LocalizeSettings("deathlink", "nameOff"),
                        DeathLinkMode.Missing => LocalizeSettings("deathlink", "nameMissing"),
                        DeathLinkMode.HullDamage => LocalizeSettings("deathlink", "nameHullDamage"),
                        DeathLinkMode.HullDamagePercent => LocalizeSettings("deathlink", "nameHullDamagePercent"),
                        _ => LocalizeSettings("deathlink", "nameDeath")
                    })
                    .SetValueWidth(_ => 115)
                    .SetTooltips(() => new List<Tooltip>
                    {
                        new TTText(LocalizeSettings("deathlink", "tooltipName")),
                        new TTText(LocalizeSettings("deathlink", "desc")),
                        new TTDivider()
                    }.Append(new TTText(Archipelago.APSaveData!.DeathLinkMode switch
                    {
                        DeathLinkMode.Off => LocalizeSettings("deathlink", "descOff"),
                        DeathLinkMode.Missing => LocalizeSettings("deathlink", "descMissing"),
                        DeathLinkMode.HullDamage => LocalizeSettings("deathlink", "descHullDamage"),
                        DeathLinkMode.HullDamagePercent => LocalizeSettings("deathlink", "descHullDamagePercent"),
                        _ => LocalizeSettings("deathlink", "descDeath")
                    }))),
                ModSettings.MakeConditional(
                    ModSettings.MakeNumericStepper(
                            () => LocalizeSettings("deathlinkHullDamage", "settingName"),
                            () => Archipelago.APSaveData!.DeathLinkHullDamage,
                            value => Archipelago.APSaveData!.DeathLinkHullDamage = value,
                            minValue: 1,
                            maxValue: 50)
                        .SetTooltips(() => new List<Tooltip>
                        {
                            new TTText(LocalizeSettings("deathlinkHullDamage", "tooltipName")),
                            new TTText(LocalizeSettings("deathlinkHullDamage", "desc"))
                        }),
                    () => Archipelago.APSaveData!.DeathLinkMode == DeathLinkMode.HullDamage
                ),
                ModSettings.MakeConditional(
                    ModSettings.MakeNumericStepper(
                            () => LocalizeSettings("deathlinkHullDamagePercent", "settingName"),
                            () => Archipelago.APSaveData!.DeathLinkHullDamagePercent,
                            value => Archipelago.APSaveData!.DeathLinkHullDamagePercent = value,
                            minValue: 5,
                            maxValue: 95,
                            step: 5)
                        .SetValueFormatter(i => $"{i}%")
                        .SetTooltips(() => new List<Tooltip>
                        {
                            new TTText(LocalizeSettings("deathlinkHullDamagePercent", "tooltipName")),
                            new TTText(LocalizeSettings("deathlinkHullDamagePercent", "desc"))
                        }),
                    () => Archipelago.APSaveData!.DeathLinkMode == DeathLinkMode.HullDamagePercent
                ),
                ModSettings.MakePadding(
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
                        }.Append(new TTText(Archipelago.APSaveData!.CardScoutMode switch
                        {
                            CardScoutMode.DontScout => LocalizeSettings("automaticScouting", "descDontScout"),
                            CardScoutMode.ScoutOnly => LocalizeSettings("automaticScouting", "descScoutOnly"),
                            _ => LocalizeSettings("automaticScouting", "descCreateHint")
                        }))), 8, 0),
                ModSettings.MakeCheckbox(
                        () => LocalizeSettings("messagesInMenu", "settingName"),
                        () => Archipelago.APSaveData!.MessagesInMenu,
                        (_, _, value) => Archipelago.APSaveData!.MessagesInMenu = value)
                    .SetTooltips(() =>
                    [
                        new TTText(LocalizeSettings("messagesInMenu", "tooltipName")),
                        new TTText(LocalizeSettings("messagesInMenu", "desc"))
                    ])
            ]).SubscribeToOnMenuClose(_ =>
            {
                APSaveData.Save();
                if (Archipelago.APSaveData!.DeathLinkMode != DeathLinkMode.Off)
                    Archipelago.DeathLinkService!.EnableDeathLink();
                else
                    Archipelago.DeathLinkService!.DisableDeathLink();
            })
        );

        // Set non-unlocked cards as unplayable
        Helper.Content.Cards.OnGetDynamicInnateCardTraitOverrides += (_, args) =>
        {
            Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
            if (Archipelago.CardToItem.TryGetValue(args.Card.GetType(), out var cardItem)
                && !Archipelago.Instance.APSaveData.HasItem(cardItem)
                // All cards are unlocked during the finale (for now at least)
                && args.State.route is not Combat { otherShip.ai: FinaleFrienemy })
            {
                args.SetOverride(ModCards.UnplayableCardTrait, true);
            }
        };

        // TextInput in main menu (easier to do through the manager rather than disconnect/reconnect every time)
        MG.inst.Window.TextInput += MainMenuPatch.OnTextInput;
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

