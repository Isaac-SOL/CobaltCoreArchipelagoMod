using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using daisyowl.text;
using HarmonyLib;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;

namespace CobaltCoreArchipelago.MenuPatches;

[HarmonyPatch(typeof(TextParser), nameof(TextParser.LayoutGlyphs))]
public class TextParserPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> storedInstructions = new(instructions);
        
        var iteratorClass = typeof(TextParser)
            .GetNestedTypes()
            .First(type => type.GetFields().Any(field => field.Name == "idx"));
        
        var textInfoClass = typeof(TextParser)
            .GetNestedTypes()
            .First(type => type.GetFields().Any(field => field.Name == "str"));

        var seqMatched = new SequenceBlockMatcher<CodeInstruction>(storedInstructions)
            .Find(
                ILMatches.Blt,
                ILMatches.Br
            )
            .PointerMatcher(SequenceMatcherRelativeElement.First)
            .Element(out var endIfBranch)
            .Advance()
            .Element(out var breakBranch)
            .Advance()
            .ExtractLabels(out _); // Remove label to go to the normal processing code, we won't need it

        // var elseLabel = (Label)elseBranch.operand;
        var breakLabel = (Label)breakBranch.operand;
        var endIfLabel = (Label)endIfBranch.operand;

        var skipOverCase1 = generator.DefineLabel();

        var seqMatched2 = seqMatched
            .Find(
                SequenceBlockMatcherFindOccurence.First,
                SequenceMatcherRelativeBounds.WholeSequence,
                ILMatches.Ldloc(5)
            )
            .EncompassUntil(
                SequenceMatcherPastBoundsDirection.After,
                ILMatches.Blt,
                ILMatches.Br
            )
            .Replace([
                // Call our function
                CodeInstruction.LoadArgument(3),
                CodeInstruction.LoadArgument(5),
                CodeInstruction.LoadLocal(5),
                CodeInstruction.LoadLocal(2),
                CodeInstruction.LoadLocal(1, true),
                CodeInstruction.LoadField(iteratorClass, "idx", true),
                CodeInstruction.LoadLocal(0, true),
                CodeInstruction.LoadField(textInfoClass, "str", true),
                CodeInstruction.Call(typeof(TextParserPatch), nameof(DiamondSubParser)),
                
                // If result is 1, reset the loop
                new CodeInstruction(OpCodes.Dup), // Copy result in case we need to check it multiple times
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Bne_Un, skipOverCase1),
                new CodeInstruction(OpCodes.Pop), // Dup was useless in this case
                new CodeInstruction(OpCodes.Br, endIfLabel),
                
                // If result is 2, break the loop
                new CodeInstruction(OpCodes.Ldc_I4_2).WithLabels(skipOverCase1),
                new CodeInstruction(OpCodes.Beq, breakLabel),
                
                // Otherwise (0), continue. This will do the normal character processing
            ]);

        foreach (var instruction in seqMatched2.AllElements())
        {
            Console.WriteLine(instruction.ToString());
        }
        
        return seqMatched2.AllElements();
    }

    enum ParserAction
    {
        Process = 0,
        Skip = 1,
        Break = 2
    }

    public static int DiamondSubParser(Color color, Func<string, uint?>? lookupColor,
                                       int currChar, Stack<uint> tagStack,
                                       ref int iteratorIdx, ref string textInfoStr)
    {
        if (currChar != '<') return (int)ParserAction.Process;

        var startIdx = iteratorIdx;
        while (iteratorIdx < textInfoStr.Length && NextChar(ref textInfoStr, ref iteratorIdx) != '>');
        
        var tagContents = textInfoStr.AsSpan(startIdx, Math.Max(0, iteratorIdx - startIdx - 1));
        if (tagContents.StartsWith("c="))
        {
            var colorStr = tagContents[2..];
            tagStack.Push(lookupColor?.Invoke(colorStr.ToString())
                          ?? TextParser.TryParseHexColor(colorStr)
                          ?? color.ToInt());
        }
        else if (tagContents.StartsWith("/c") && tagStack.Count > 0)
        {
            tagStack.Pop();
        }
        else
        {
            iteratorIdx = startIdx;
            return (int)ParserAction.Process;
        }
        if (iteratorIdx >= textInfoStr.Length)
            return (int)ParserAction.Break;
        return (int)ParserAction.Skip;
    }
    
    private static int NextChar(ref string textInfoStr, ref int iteratorIdx)
    {
        var utf32 = char.ConvertToUtf32(textInfoStr, iteratorIdx);
        iteratorIdx += char.IsSurrogatePair(textInfoStr, iteratorIdx) ? 2 : 1;
        return utf32;
    }
}