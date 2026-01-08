using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;

namespace CobaltCoreArchipelago.MenuPatches;

public class VaultPatches;

// Change future memory unlock condition
[HarmonyPatch(typeof(Vault), nameof(Vault.Render))]
public class VaultRenderPatch
{
    [HarmonyDebug]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
                                                          ILGenerator generator)
    {
        List<CodeInstruction> storedInstructions = new(instructions);
        var codeMatcher = new CodeMatcher(storedInstructions, generator);
        // Remove the part where vaultMemories are checked, and replace with our own check
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
        if (Archipelago.InstanceSlotData.WinCondition == WinCondition.TotalMemories)
        {
            return vaultMemories
                       .Sum(memorySet => memorySet.memoryKeys
                                .Sum(entry => entry.unlocked ? 1 : 0))
                   > Archipelago.InstanceSlotData.WinReqTotal;
        }
        
        // WinCondition.PerCharacterMemories
        return vaultMemories
            .All(memorySet => memorySet.memoryKeys
                     .Sum(entry => entry.unlocked ? 1 : 0) > Archipelago.InstanceSlotData.WinReqPerChar);
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
