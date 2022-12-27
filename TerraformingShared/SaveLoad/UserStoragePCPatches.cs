using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Terraforming;
using Terraforming.WorldStreaming;

#if !BelowZero
namespace TerraformingShared.SaveLoad
{
    [HarmonyPatch(typeof(UserStoragePC))]
    static class UserStoragePCPatches
    {
        [HarmonyPatch(nameof(UserStoragePC.UpgradeSaveData))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> RemoveDeletingOctrees(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase originalMethod)
        {
            var codeMatcherCursor = new CodeMatcher(instructions);
            
            Action<CodeMatcher, ILGenerator>[] patchers =
            {
                AssignEmptyArrayToOctreesLocal
            };

            try
            {
                foreach (var patcher in patchers)
                {
                    patcher.Invoke(codeMatcherCursor, generator);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());

                codeMatcherCursor.ReportFailure(originalMethod, Logger.Error);

                return instructions;
            }

            return codeMatcherCursor.InstructionEnumeration();
        }

        private static void AssignEmptyArrayToOctreesLocal(CodeMatcher codeCursor, ILGenerator generator)
        {
            List<Label> labels;

            codeCursor.Start();
            codeCursor.MatchForward(useEnd: false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldstr, "compiled-batch-*.optoctrees"),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Directory), nameof(Directory.GetFiles), new[] { typeof(string), typeof(string), typeof(SearchOption) }))
            );
            codeCursor.ThrowIfInvalid("Could not find call to Directory.GetFiles with octrees search pattern");

            labels = codeCursor.Instruction.ExtractLabels();

            codeCursor.RemoveInstructions(4)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UserStoragePCPatches), nameof(GetEmptyArray))).WithLabels(labels));
        }

        private static string[] GetEmptyArray()
        {
            return new string[] { };
        }

        [HarmonyPatch(nameof(UserStoragePC.UpgradeSaveDataAsync))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> SkipOctrees(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase originalMethod)
        {
            var codeMatcherCursor = new CodeMatcher(instructions);

            Action<CodeMatcher, ILGenerator>[] patchers =
            {
                RemoveOctreesPattern
            };

            try
            {
                foreach (var patcher in patchers)
                {
                    patcher.Invoke(codeMatcherCursor, generator);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());

                codeMatcherCursor.ReportFailure(originalMethod, Logger.Error);

                return instructions;
            }

            return codeMatcherCursor.InstructionEnumeration();
        }

        private static void RemoveOctreesPattern(CodeMatcher codeCursor, ILGenerator generator)
        {
            List<Label> labels;

            codeCursor.Start();
            codeCursor.MatchForward(useEnd: false,
                new CodeMatch(OpCodes.Brtrue),
                new CodeMatch(OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Ldstr, "compiled-batch-*.optoctrees"),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Directory), nameof(Directory.EnumerateFiles), new[] { typeof(string), typeof(string), typeof(SearchOption) })),
                new CodeMatch(OpCodes.Call)
            )
            .ThrowIfInvalid("Could not find condition with call to Directory.EnumerateFiles uses octrees search pattern");

            labels = codeCursor.Instruction.ExtractLabels();

            codeCursor.RemoveInstructions(6);

            codeCursor.Instruction.WithLabels(labels);
        }
    }
}
#endif
