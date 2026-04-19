using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using Archipelago.MultiClient.Net.Enums;
using daisyowl.text;
using HarmonyLib;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;

namespace CobaltCoreArchipelago.Features;

internal class RouteOverlay
{
    internal static Spr CatMiniTalkingSpr;
    
    internal APShout? currentShout;
    
    internal static RouteOverlay MakeNew()
    {
        GRenderPostfix.overlay = new RouteOverlay();
        return GRenderPostfix.overlay;
    }

    internal static void Remove()
    {
        GRenderPostfix.overlay = null;
    }

    internal static bool CompIsBackup(State s) => s.IsOutsideRun()
                                                  || s.route is Combat { otherShip.ai: TheCobalt }
                                                  || s.characters.Any(c => c.deckType == Deck.colorless);
    
    internal void Render(G g, State s)
    {
        var catBackup = CompIsBackup(s);
        string CBU(string str) => catBackup ? str.ToUpperInvariant() : str;
        
        if (Archipelago.Instance.MessagesToAnnounce.Count > 0)
        {
            if (currentShout is null)
            {
                // There is a message to display and no message currently being shown:
                // Load the message as a string. This is run only once per message
                var message = Archipelago.Instance.MessagesToAnnounce[0];
                var item = message.item!;
                string messageStr;
                if (message.type == MessageToAnnounce.DeathlinkReceived)
                {
                    messageStr = AdaptiveShoutCache.GetLocalizedRandomLine(
                        ["compShouts", "deathlink"],
                        catBackup: catBackup,
                        $"<c={APColors.OtherPlayer}>{CBU(message.deathlink!.Source)}</c>",
                        CBU(message.deathlink!.Cause)
                    );
                }
                else if (Archipelago.ItemToCard.TryGetValue(item.ItemName, out var cardType))
                {
                    var cardDeck = DB.cardMetas[cardType.Name].deck;
                    messageStr = AdaptiveShoutCache.GetLocalizedRandomLine(
                        ["compShouts", "card", "character"],
                        catBackup: catBackup,
                        $"<c=card>{CBU(item.ItemName)}</c>",
                        $"<c={APColors.FromPlayerName(item.Player.Name)}>{CBU(item.Player.Name)}</c>",
                        $"<c={Colors.LookupColor(cardDeck.Key())}>{CBU(Character.GetDisplayName(cardDeck, s))}</c>"
                    );
                }
                else if (Archipelago.ItemToArtifact.TryGetValue(item.ItemName, out var artifactType))
                {
                    List<string> key = ["compShouts", "artifact"];
                    if (ArtifactValidForAddition(s, artifactType, item.ItemName, item.Player.Name))
                        key.Add("instant");
                    messageStr = AdaptiveShoutCache.GetLocalizedRandomLine(
                        key.ToArray(),
                        catBackup: catBackup,
                        $"<c=artifact>{CBU(item.ItemName)}</c>",
                        $"<c={APColors.FromPlayerName(item.Player.Name)}>{CBU(item.Player.Name)}</c>"
                    );
                }
                else if (Archipelago.ItemToDeck.TryGetValue(item.ItemName, out var deck))
                {
                    var deckKey = deck.Key();
                    messageStr = AdaptiveShoutCache.GetLocalizedRandomLine(
                        ["compShouts", "character", deckKey],
                        catBackup: catBackup,
                        $"<c={Colors.LookupColor(deckKey) ?? 0xFFFFFFFF}>{CBU(item.ItemName)}</c>",
                        $"<c={APColors.FromPlayerName(item.Player.Name)}>{CBU(item.Player.Name)}</c>"
                    );
                }
                else if (Archipelago.ItemToMemory.TryGetValue(item.ItemName, out var deckMemory))
                {
                    var deckKey = deckMemory.Key();
                    messageStr = AdaptiveShoutCache.GetLocalizedRandomLine(
                        ["compShouts", "memory", deckKey],
                        catBackup: catBackup,
                        $"<c={APColors.Progression}>{CBU(item.ItemName)}</c>",
                        $"<c={APColors.FromPlayerName(item.Player.Name)}>{CBU(item.Player.Name)}</c>",
                        $"<c={Colors.LookupColor(deckKey)}>{CBU(Character.GetDisplayName(deckMemory, s))}</c>"
                    );
                }
                else if ((item.Flags & ItemFlags.Trap) != ItemFlags.None)
                {
                    messageStr = AdaptiveShoutCache.GetLocalizedRandomLine(
                        ["compShouts", "trap", item.ItemName],
                        catBackup: catBackup,
                        $"<c={APColors.Trap}>{CBU(item.ItemName)}</c>",
                        $"<c={APColors.FromPlayerName(item.Player.Name)}>{CBU(item.Player.Name)}</c>"
                    );
                }
                else if (item.Flags == ItemFlags.None)
                {
                    messageStr = AdaptiveShoutCache.GetLocalizedRandomLine(
                        ["compShouts", "filler", item.ItemName],
                        catBackup: catBackup,
                        $"<c={APColors.Trap}>{CBU(item.ItemName)}</c>",
                        $"<c={APColors.FromPlayerName(item.Player.Name)}>{CBU(item.Player.Name)}</c>"
                    );
                }
                else
                {
                    messageStr = AdaptiveShoutCache.GetLocalizedRandomLine(
                        ["compShouts"],
                        catBackup: catBackup,
                        $"<c={APColors.FromFlags(item.Flags)}>{CBU(item.ItemName)}</c>",
                        $"<c={APColors.FromPlayerName(item.Player.Name)}>{CBU(item.Player.Name)}</c>"
                    );
                }
                currentShout = new APShout
                {
                    message = messageStr
                };
                Archipelago.Instance.MessagesToAnnounce.RemoveAt(0);
            }
            else
            {
                // There is a message currently being shown and more to display
                currentShout.SkipToEnd();
            }
        }
        currentShout?.Update(g);
        if (currentShout is not null && currentShout.progress > currentShout.message.Length + 200.0)
            currentShout = null;
        if (currentShout is not null && currentShout.delay == 0.0)
        {
            var textRect = Draw.Text(currentShout.message, 0, 0, maxWidth: 230.0, align: TAlign.Left, dontDraw: true);
            var yOffset = Math.Max(textRect.h - 14.0, 0);
            Blurbs.Render(
                g,
                currentShout.message,
                x: 94.0 + g.cornerMenu.GetExtraOffset(), y: 20.0 + yOffset,
                dir: BlurbDir.Right,
                align: -0.4,
                progress: currentShout.progress,
                textColor: Colors.textBold,
                borderColor: DB.decks[Deck.colorless].color,
                maxWidth: 230.0,
                showStem: CompRenderPostfix.CanShowText(g)
            );
        }
    }

    private static bool ArtifactValidForAddition(State state, Type artifactType, string itemName, string itemSender)
    {
        if (state.IsOutsideRun()) return false;
        Debug.Assert(Archipelago.Instance.APSaveData != null, "Archipelago.Instance.APSaveData != null");
        var newArtifact = (Artifact)artifactType.CreateInstance();
        var newArtifactMeta = newArtifact.GetMeta();
        var local = itemSender == Archipelago.Instance.APSaveData.Slot;
        var hasDeck = state.characters.Any(character => character.deckType == newArtifactMeta.owner);
        return Archipelago.InstanceSlotData.ImmediateArtifactRewards switch
            {
                CardRewardsMode.IfLocal => local,
                CardRewardsMode.IfHasDeck => hasDeck,
                CardRewardsMode.IfLocalAndHasDeck => local && hasDeck,
                CardRewardsMode.Always => true,
                _ => false
            } && !Archipelago.InstanceSlotData.ImmediateRewardsBlacklist.Contains(itemName)
              && !ArtifactReward.GetBlockedArtifacts(state).Contains(artifactType);
    }
}

[HarmonyPatch(typeof(G), nameof(G.Render))]
public static class GRenderPostfix
{
    internal static RouteOverlay? overlay;
    
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> storedInstructions = new(instructions);

        // Using Shrike here because I don't know how to use indices with CodeMatcher
        var seqMatched = new SequenceBlockMatcher<CodeInstruction>(storedInstructions)
            // Match sequence where the function adds the tooltip by checking showUnlockInstructions
            .Find(
                ILMatches.Ldarg(0),
                ILMatches.Call(nameof(G.Pop))
            )
            .Insert(
                SequenceMatcherPastBoundsDirection.After,
                SequenceMatcherInsertionResultingBounds.ExcludingInsertion,
                CodeInstruction.LoadArgument(0), // this
                CodeInstruction.LoadArgument(0),
                CodeInstruction.LoadField(typeof(G), nameof(G.state)), // this.state
                CodeInstruction.Call((G g, State s) => RenderOverlay(g, s))
            );
        
        return seqMatched.AllElements();
    }

    public static void RenderOverlay(G g, State s)
    {
        overlay?.Render(g, s);
    }
}

[HarmonyPatch(typeof(Character), nameof(Character.DrawFace))]
public static class CompRenderPostfix
{
    public static bool CanShowText(G g) =>
        g.metaRoute is null
        && ((g.state.routeOverride is null && g.state.route.GetShowCockpit())
            || (g.state.routeOverride is not null && g.state.routeOverride.GetShowCockpit()));
    
    public static void Postfix(Character __instance, G g, double x, double y, bool mini)
    {
        if ((!GRenderPostfix.overlay?.currentShout?.IsDonePrinting() ?? false)
            && __instance.type == "comp"
            && mini
            && !RouteOverlay.CompIsBackup(g.state)
            && g.time % (1.0 / 3.0) > (1.0 / 6.0))
        {
            Draw.Sprite(RouteOverlay.CatMiniTalkingSpr, x + 5.0, y + 3.0);
        }
    }
}
