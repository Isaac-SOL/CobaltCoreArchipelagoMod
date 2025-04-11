using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DemoMod.External;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace DemoMod.Features;

public class KnowledgeManager : IKokoroApi.IV2.IStatusRenderingApi.IHook
{
    private static IModSoundEntry _lessonLearnedSound = null!;
    
    public KnowledgeManager(IPluginPackage<IModManifest> package, IModHelper helper)
    {
        ModEntry.Instance.KokoroApi.StatusRendering.RegisterHook(this);
        
        /*
         * There are times in which you need to hook code onto after a method.
         * Harmony allows us to achieve this on any method.
         * It is advised to name arguments for Harmony patches, to make them unambiguous to the reader.
         */
        ModEntry.Instance.Harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(AStatus), nameof(AStatus.Begin)),
            postfix: new HarmonyMethod(GetType(), nameof(AStatus_Begin_Postfix))
        );

        _lessonLearnedSound = helper.Content.Audio.RegisterSound(package.PackageRoot.GetRelativeFile("assets/LessonLearned.wav"));
    }

    /*
     * As an IStatusRenderingApi.IHook, the KnowledgeManager has the power to make any status render as bars.
     * However, it only knows how to render Knowledge as bars - so it has its definition for Knowledge,
     * and returns null for everything else.
     */
    public (IReadOnlyList<Color> Colors, int? BarSegmentWidth)? OverrideStatusRenderingAsBars(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusRenderingAsBarsArgs args)
    {
        if (args.Status != ModEntry.Instance.KnowledgeStatus.Status) return null;

        var ship = args.Ship;
        var expected = GetKnowledgeLimit(ship);
        var current = ship.Get(ModEntry.Instance.KnowledgeStatus.Status);

        var filled = Math.Min(expected, current);
        var empty = Math.Max(expected - current, 0);
        var overflow = Math.Max(current - expected, 0);

        return (Enumerable.Repeat(new Color("fbb954"), filled)
                .Concat(Enumerable.Repeat(new Color("7a3045"), empty)
                .Concat(Enumerable.Repeat(new Color("f78716"), overflow)))
                .ToImmutableList(),
            null);
    }

    /*
     * Harmony Postfixes have access to all the arguments of the original method.
     * The arguments must be the exact same name as the original, but the type can either be the type or any supertype.
     * There are also special arguments that can be added.
     * __instance is the "this" in the original call.
     */
    public static void AStatus_Begin_Postfix(AStatus __instance, State s, Combat c)
    {
        if (__instance.status != ModEntry.Instance.KnowledgeStatus.Status) return;
        
        var ship = __instance.targetPlayer ? s.ship : c.otherShip;
        if (ship.Get(ModEntry.Instance.KnowledgeStatus.Status) < GetKnowledgeLimit(ship)) return;
        
        c.QueueImmediate([
            new AStatus
            {
                status = Status.powerdrive,
                statusAmount = 1,
                targetPlayer = __instance.targetPlayer,
                timer = 0
            }.Silent(),
            new AStatus
            {
                status = ModEntry.Instance.LessonStatus.Status,
                statusAmount = 1,
                targetPlayer = __instance.targetPlayer,
                timer = 0
            }.Silent(),
            new AStatus
            {
                status = ModEntry.Instance.KnowledgeStatus.Status,
                statusAmount = -GetKnowledgeLimit(ship),
                targetPlayer = __instance.targetPlayer
            }.Silent()
        ]);
        _lessonLearnedSound.CreateInstance();
    }

    public static int GetKnowledgeLimit(Ship ship)
    {
        return 3 + ship.Get(ModEntry.Instance.LessonStatus.Status);
    }
}