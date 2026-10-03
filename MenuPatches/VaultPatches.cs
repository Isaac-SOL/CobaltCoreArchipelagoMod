using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using daisyowl.text;
using HarmonyLib;
using Microsoft.Extensions.Logging;

namespace CobaltCoreArchipelago.MenuPatches;

public class VaultPatches;

// Change future memory unlock condition
[HarmonyPatch(typeof(Vault), nameof(Vault.Render))]
public class VaultRenderPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
                                                          ILGenerator generator)
    {
        List<CodeInstruction> storedInstructions = new(instructions);
        var codeMatcher = new CodeMatcher(storedInstructions, generator);
        // Remove the part where vaultMemories are checked for Future Memory unlock, and replace with our own check
        codeMatcher.MatchStartForward(
                CodeMatch.WithOpcodes([OpCodes.Ldloc_0]),
                // NOTE: These 2 instructions are seemingly added by Nickel to account for more characters. We are overriding that
                CodeMatch.LoadsConstant(6),
                CodeMatch.WithOpcodes([OpCodes.Call]),
                
                CodeMatch.WithOpcodes([OpCodes.Ldsfld]),
                CodeMatch.WithOpcodes([OpCodes.Dup]),
                CodeMatch.WithOpcodes([OpCodes.Brtrue_S])
            ).ThrowIfInvalid("Could not find vaultMemories check instructions")
            .Advance()  // We keep the ldloc.0 as is because it has 2 labels jumping to it
            .RemoveInstructions(23)
            .InsertAndAdvance(
                CodeInstruction.Call<List<Vault.MemorySet>, bool>(vaultMemories => CanCompleteGame(vaultMemories)),
                CodeInstruction.StoreLocal(2)
            );
        return codeMatcher.Instructions();
    }

    internal static bool CanCompleteGame(List<Vault.MemorySet> vaultMemories)
    {
        var countValid = vaultMemories
                             .Sum(memorySet => memorySet.memoryKeys.Count(entry => entry.unlocked))
                         >= Archipelago.InstanceSlotData.WinReqTotal;
        var charactersValid = vaultMemories
                                  .Count(memorySet => memorySet.memoryKeys.Count(entry => entry.unlocked)
                                                      >= Archipelago.InstanceSlotData.WinReqPerChar)
                              >= Archipelago.InstanceSlotData.CharactersRequired;
        return countValid && charactersValid;
    }

    public static void Postfix(Vault __instance, G g)
    {
        if (__instance.introAnimTime < 2.0) return;
        var slideIn = Vault.GetSlideIn(__instance.introAnimTime - 2.0);
        var memories = Vault.GetVaultMemories(g.state);
        var totalReq = Archipelago.InstanceSlotData.WinReqTotal;
        var perCharReq = Archipelago.InstanceSlotData.WinReqPerChar;
        var charsReq = Archipelago.InstanceSlotData.CharactersRequired;
        string goalString;
        var mixed = false;
        if (totalReq <= perCharReq * charsReq)
        {
            // Total memory amount is subsumed by per-character settings
            var completed = memories.Count(memorySet => memorySet.memoryKeys.Count(entry => entry.unlocked) >= perCharReq);
            goalString = string.Format(
                ModEntry.Instance.Localizations.Localize([
                    "vault", perCharReq > 1
                        ? "goalPerCharacterMemories"
                        : "goalPerCharacterMemoriesSingular"
                ]),
                perCharReq, completed, charsReq);
        }
        else if (totalReq / 3 + (totalReq % 3 > 0 ? 1 : 0) >= charsReq)
        {
            // Per-character settings are subsumed by total memory amount
            var found = memories.Sum(memorySet => memorySet.memoryKeys.Count(entry => entry.unlocked));
            goalString = string.Format(
                ModEntry.Instance.Localizations.Localize(["vault", "goalTotalMemories"]),
                found, totalReq);
        }
        else
        {
            // Mixed goal
            var charsCompleted = memories.Count(memorySet => memorySet.memoryKeys.Count(entry => entry.unlocked) >= perCharReq);
            var totalFound = memories.Sum(memorySet => memorySet.memoryKeys.Count(entry => entry.unlocked));
            goalString = string.Format(
                ModEntry.Instance.Localizations.Localize([
                    "vault", perCharReq > 1
                        ? "goalMixed"
                        : "goalMixedSingular"
                ]),
                totalFound, totalReq, perCharReq, charsCompleted, charsReq);
            mixed = true;
        }

        Draw.Text(goalString,
                  123.0, (mixed ? 215.0 : 228.0) + slideIn,
                  align: TAlign.Center, color: Colors.buttonBoxNormal, outline: Colors.black);
    }
}

// Force being able to go out of Vault screen
[HarmonyPatch(typeof(Vault), nameof(Vault.GetCanContinue))]
public static class VaultContinuePatch
{
    public static void Postfix(ref bool __result)
    {
        __result = true;
    }
}
