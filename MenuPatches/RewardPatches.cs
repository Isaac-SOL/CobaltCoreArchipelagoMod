using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using CobaltCoreArchipelago.Artifacts;
using CobaltCoreArchipelago.Cards;
using daisyowl.text;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;

namespace CobaltCoreArchipelago.MenuPatches;

public class RewardPatches;

[HarmonyPatch(typeof(CardReward), nameof(CardReward.Render))]
public static class CardRewardRenderPatch
{
    private static double rescoutTimer = 0.0;
    
    public static void Prefix(CardReward __instance)
    {
        // Reset the time when we have a new CardReward screen
        // This is important because we don't want to overlap the first scouting done by CardOfferingPatch
        if (!__instance.didFirstFrame)
            rescoutTimer = 0.0;
    }

    public static void Postfix(CardReward __instance, G g)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        
        // Show tip about AP cards
        if (__instance.cards.Any(card => card is CheckLocationCard))
        {
            Draw.Text(ModEntry.Instance.Localizations.Localize(["cardReward", "apCardsTip"]),
                      240, 245, align: TAlign.Center, color: Colors.textMain, outline: Colors.black);
        }
        
        if (Archipelago.Instance.APSaveData.CardScoutMode == CardScoutMode.DontScout) return;

        // Recheck AP checks every 5 seconds
        if (rescoutTimer > 5.0)
        {
            rescoutTimer -= 5.0;

            var checkCards = __instance.cards
                .Where(card => card is CheckLocationCard)
                .Cast<CheckLocationCard>().ToList();
            var locations = checkCards.Select(card => card.locationName).ToArray();

            if (locations.Length > 0)
            {
                Archipelago.Instance.ScoutLocationInfo(locations).ContinueWith(task =>
                {
                    for (var i = 0; i < checkCards.Count; i++)
                        checkCards[i].LoadInfo(task.Result[i]);
                });
            }
        }
        rescoutTimer += g.dt;
    }
}

[HarmonyPatch(typeof(ArtifactReward), nameof(ArtifactReward.Render))]
public static class ArtifactRewardRenderPatch
{
    private static double rescoutTimer = 0.0;

    public static void Prefix(ArtifactReward __instance)
    {
        // Reset the time when we have a new ArtifactReward screen
        // This is important because we don't want to overlap the first scouting done by ArtifactOfferingPatch
        if (!__instance.playedIntroSound)
            rescoutTimer = 0.0;
    }

    public static void Postfix(ArtifactReward __instance, G g)
    {
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        
        if (Archipelago.Instance.APSaveData.CardScoutMode == CardScoutMode.DontScout) return;
        
        // Recheck AP checks every 5 seconds
        if (rescoutTimer > 5.0)
        {
            rescoutTimer -= 5.0;
        
            var checkArtifacts = __instance.artifacts
                .Where(artifact => artifact is CheckLocationArtifact)
                .Cast<CheckLocationArtifact>().ToList();
            var locations = checkArtifacts.SelectMany(artifact => artifact.locationName).ToArray();

            if (locations.Length > 0)
            {
                Archipelago.Instance.ScoutLocationInfo(locations).ContinueWith(task =>
                {
                    for (var i = 0; i < checkArtifacts.Count; i++)
                        checkArtifacts[i].LoadInfo(task.Result?.GetSlice(i).ToArray());
                });
            }
        }
        rescoutTimer += g.dt;
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> storedInstructions = new(instructions);

        var seqMatched = new SequenceBlockMatcher<CodeInstruction>(storedInstructions)
            .Find(
                ILMatches.Call(typeof(Artifact).GetMethod(nameof(Artifact.GetLocName))!)
            )
            .PointerMatcher(SequenceMatcherRelativeElement.First)
            .Insert(
                SequenceMatcherPastBoundsDirection.After,
                SequenceMatcherInsertionResultingBounds.ExcludingInsertion,
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ldloc_S, 15),
                CodeInstruction.Call((Artifact a) => GetArtifactName(a))
            )
            .Find(
                ILMatches.Call(typeof(string).GetMethods()
                                   .First(m => m.Name == nameof(string.Concat)
                                               && m.GetParameters().Length == 2
                                               && m.GetParameters().All(p => p.ParameterType == typeof(string)))),
                ILMatches.Ldloc(22),
                ILMatches.Ldfld(typeof(Vec).GetField(nameof(Vec.x))!)
            )
            .PointerMatcher(SequenceMatcherRelativeElement.First)
            .Insert(
                SequenceMatcherPastBoundsDirection.After,
                SequenceMatcherInsertionResultingBounds.ExcludingInsertion,
                new CodeInstruction(OpCodes.Ldloc_S, 15),
                CodeInstruction.Call((string s, Artifact a) => GetArtifactSubtitle(s, a))
            );
        
        return seqMatched.AllElements();
    }

    public static string GetArtifactName(Artifact artifact)
    {
        if (artifact is CheckLocationArtifact apArtifact
            && apArtifact.locationItemName[0] is not null)
        {
            if (apArtifact.locationItemName[1] is not null)
            {
                var effItemName1 = apArtifact.locationItemName[0]!;
                var effItemName2 = apArtifact.locationItemName[1]!;
                var charsToRemove = effItemName1.Length + effItemName2.Length - 22;
                if (charsToRemove > 0)
                {
                    var diff = Math.Abs(effItemName1.Length - effItemName2.Length);
                    var halfRest = (charsToRemove - diff) / 2;
                    var charsToRemove1 = effItemName1.Length > effItemName2.Length ? diff + halfRest : halfRest;
                    var charsToRemove2 = effItemName1.Length < effItemName2.Length ? diff + halfRest : halfRest;
                    if (charsToRemove1 > 0) effItemName1 = effItemName1.Remove(effItemName1.Length - charsToRemove1) + "...";
                    if (charsToRemove2 > 0) effItemName2 = effItemName2.Remove(effItemName2.Length - charsToRemove2) + "...";
                }
                return (effItemName1 + " & " + effItemName2).ToUpper();
            }
            else
            {
                var charsToRemove = apArtifact.locationItemName[0]!.Length - 25;
                var effItemName = apArtifact.locationItemName[0]!;
                if (charsToRemove > 0)
                    effItemName = effItemName.Remove(Math.Max(effItemName.Length - charsToRemove, 0)) + "...";
                return effItemName.ToUpper();
            }
        }

        return artifact.GetLocName();
    }

    public static string GetArtifactSubtitle(string baseSubtitle, Artifact artifact)
    {
        if (artifact is CheckLocationArtifact apArtifact
            && apArtifact.locationSlotName[0] is not null)
        {
            if (apArtifact.locationSlotName[1] is not null)
            {
                var effSlotName1 = apArtifact.locationSlotName[0]!;
                var effSlotName2 = apArtifact.locationSlotName[1]!;
                var charsToRemove = effSlotName1.Length + effSlotName2.Length - 20;
                if (charsToRemove > 0)
                {
                    var diff = Math.Abs(effSlotName1.Length - effSlotName2.Length);
                    var halfRest = (charsToRemove - diff) / 2;
                    var charsToRemove1 = effSlotName1.Length > effSlotName2.Length ? diff + halfRest : halfRest;
                    var charsToRemove2 = effSlotName1.Length < effSlotName2.Length ? diff + halfRest : halfRest;
                    if (charsToRemove1 > 0) effSlotName1 = effSlotName1.Remove(effSlotName1.Length - charsToRemove1) + "...";
                    if (charsToRemove2 > 0) effSlotName2 = effSlotName2.Remove(effSlotName2.Length - charsToRemove2) + "...";
                }
                return string.Format(
                    ModEntry.Instance.Localizations.Localize(["artifactReward", "twoItemsCharNames"]),
                    effSlotName1, effSlotName2);
            }
            else
            {
                var charsToRemove = apArtifact.locationSlotName[0]!.Length - 20;
                var effPlayerName = apArtifact.locationSlotName[0]!;
                if (charsToRemove > 0)
                    effPlayerName = effPlayerName.Remove(Math.Max(effPlayerName.Length - charsToRemove, 0)) + "...";
                return string.Format(
                    ModEntry.Instance.Localizations.Localize(["artifactReward", "oneItemCharName"]),
                    effPlayerName);
            }
        }

        return baseSubtitle;
    }
}

