using HarmonyLib;
using HarmonyLib.Tools;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using Terraforming.Tools.BuilderPatches;
using UnityEngine;

#if !BelowZero
namespace TerraformingShared.Tools
{
    [HarmonyPatch(typeof(BaseDeconstructable))]
    static class BaseDeconstructablePatches
    {
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(BaseDeconstructable.DeconstructionAllowed))]
        static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codeMatcherCursor = new CodeMatcher(instructions);

            PatchClearFromConstructionObstacles(codeMatcherCursor, generator);
            if (codeMatcherCursor.IsInvalid)
            {
                return instructions;
            }

            return codeMatcherCursor.InstructionEnumeration();
        }

        static void PatchClearFromConstructionObstacles(CodeMatcher codeCursor, ILGenerator generator)
        {
            codeCursor.Start();
            codeCursor.MatchForward(useEnd: true,
                new CodeMatch(OpCodes.Call, AccessTools.Method($"{typeof(Builder)}:{nameof(Builder.GetObstacles)}"))
            );
            codeCursor.Advance(1);

            if (codeCursor.IsValid)
            {
                var labels = codeCursor.Instruction.ExtractLabels();

                codeCursor.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(BaseDeconstructable), nameof(BaseDeconstructable.sObstacleGameObjects))).WithLabels(labels),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method($"{typeof(BuilderExtensions)}:{nameof(BuilderExtensions.ClearConstructionObstacles)}", new Type[] { typeof(List<GameObject>) }))
                );
            }
        }
    }
}
#endif
