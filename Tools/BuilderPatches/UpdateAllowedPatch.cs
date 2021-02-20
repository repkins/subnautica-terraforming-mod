using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Terraforming.Tools.BuilderPatches
{
    [HarmonyPatch(typeof(Builder))]
    [HarmonyPatch(nameof(Builder.UpdateAllowed))]
    static class UpdateAllowedPatch
    {
        static Material destroyableObstacleMat = null;

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            if (Config.Instance.destroyLargerObstaclesOnConstruction)
            {
                var codeMatcherCursor = new CodeMatcher(instructions);

                PatchClearFromConstructionObstacles(codeMatcherCursor);
                if (codeMatcherCursor.IsInvalid)
                {
                    codeMatcherCursor.ReportFailure(AccessTools.Method(typeof(Builder), nameof(Builder.UpdateAllowed)), Logger.Warning);
                    return instructions;
                }

                PatchHighlightInRedConstructionObstacles(codeMatcherCursor, generator);
                if (codeMatcherCursor.IsInvalid)
                {
                    codeMatcherCursor.ReportFailure(AccessTools.Method(typeof(Builder), nameof(Builder.UpdateAllowed)), Logger.Warning);
                    return instructions;
                }

                return codeMatcherCursor.InstructionEnumeration();
            }

            return instructions;
        }

        static void PatchClearFromConstructionObstacles(CodeMatcher codeCursor)
        {
            codeCursor.Start();
            codeCursor.MatchForward(false,
                new CodeMatch(OpCodes.Ldloc_1),
                new CodeMatch(OpCodes.Ldloc_3),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<GameObject>), nameof(List<GameObject>.Count))),
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(OpCodes.Ceq),
                new CodeMatch(OpCodes.And),
                new CodeMatch(OpCodes.Stloc_1)
            );

            if (codeCursor.IsValid)
            {
                var labels = codeCursor.Instruction.ExtractLabels();
                codeCursor.RemoveInstructions(7);
                codeCursor.AddLabels(labels);

                codeCursor.MatchForward(true,
                    new CodeMatch(OpCodes.Ldloc_3),
                    new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<GameObject>), nameof(List<GameObject>.Count))),
                    new CodeMatch(OpCodes.Ldc_I4_0),
                    new CodeMatch(OpCodes.Ble)
                );

                if (codeCursor.IsValid)
                {
                    var countCondBranchOperand = (Label)codeCursor.Operand;
                    codeCursor.MatchForward(false, new CodeMatch(instruction => instruction.labels.Contains(countCondBranchOperand)));

                    if (codeCursor.IsValid)
                    {
                        labels = codeCursor.Instruction.ExtractLabels();

                        codeCursor.InsertAndAdvance(
                            new CodeInstruction(OpCodes.Ldloc_3).WithLabels(labels),
                            new CodeInstruction(OpCodes.Call, AccessTools.Method($"{typeof(UpdateAllowedPatch)}:{nameof(ClearConstructionObstacles)}", new Type[] { typeof(List<GameObject>) }))
                        );

                        codeCursor.InsertAndAdvance(
                            new CodeInstruction(OpCodes.Ldloc_1),
                            new CodeInstruction(OpCodes.Ldloc_3),
                            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<GameObject>), nameof(List<GameObject>.Count))),
                            new CodeInstruction(OpCodes.Ldc_I4_0),
                            new CodeInstruction(OpCodes.Ceq),
                            new CodeInstruction(OpCodes.And),
                            new CodeInstruction(OpCodes.Stloc_1)
                        );
                    }
                }
            }
        }

        static void PatchHighlightInRedConstructionObstacles(CodeMatcher codeMatcherCursor, ILGenerator generator)
        {
            codeMatcherCursor.Start();
            codeMatcherCursor.MatchForward(true,
                new CodeMatch(OpCodes.Ldloc_3),
                new CodeMatch(OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<GameObject>), "Item")),
                new CodeMatch(OpCodes.Stloc_S)
                );

            if (codeMatcherCursor.IsValid)
            {
                var obstacleLocal = (LocalBuilder)codeMatcherCursor.Operand;

                codeMatcherCursor.MatchForward(false,
                    new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(Builder), nameof(Builder.obstaclesBuffer))),
                    new CodeMatch(OpCodes.Ldloc_S), // Renderer
                    new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(Builder), nameof(Builder.builderObstacleMaterial))),
                    new CodeMatch(OpCodes.Ldloc_S), // int
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(CommandBuffer), nameof(CommandBuffer.DrawRenderer), new Type[] { typeof(Renderer), typeof(Material), typeof(int) }))
                );

                if (codeMatcherCursor.IsValid)
                {
                    var rendererLocal = (LocalBuilder)codeMatcherCursor.Advance(1).Operand;
                    var submeshIndexLocal = (LocalBuilder)codeMatcherCursor.Advance(2).Operand;

                    codeMatcherCursor.Advance(-3);

                    var notConstructionObstacleLabel = generator.DefineLabel();
                    codeMatcherCursor
                        .InsertAndAdvance(
                            new CodeInstruction(OpCodes.Ldloc_S, obstacleLocal),
                            new CodeInstruction(OpCodes.Call, AccessTools.Method($"{typeof(UpdateAllowedPatch)}:{nameof(IsConstructionObstacle)}", new Type[] { typeof(GameObject) })),
                            new CodeInstruction(OpCodes.Brfalse_S, notConstructionObstacleLabel)
                        );

                    codeMatcherCursor
                        .InsertAndAdvance(
                            new CodeInstruction(OpCodes.Call, AccessTools.Method($"{typeof(UpdateAllowedPatch)}:{nameof(DefineDestroyMaterialIfNot)}"))
                        );

                    codeMatcherCursor
                        .InsertAndAdvance(
                            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Builder), nameof(Builder.obstaclesBuffer))),
                            new CodeInstruction(OpCodes.Ldloc_S, rendererLocal), // Renderer
                            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(UpdateAllowedPatch), nameof(destroyableObstacleMat))),
                            new CodeInstruction(OpCodes.Ldloc_S, submeshIndexLocal), // int
                            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(CommandBuffer), nameof(CommandBuffer.DrawRenderer), new Type[] { typeof(Renderer), typeof(Material), typeof(int) }))
                        );

                    var isConstructionObstacleLabel = generator.DefineLabel();
                    codeMatcherCursor
                        .InsertAndAdvance(
                            new CodeInstruction(OpCodes.Br_S, isConstructionObstacleLabel)
                        );

                    codeMatcherCursor.Instruction.WithLabels(notConstructionObstacleLabel);

                    codeMatcherCursor.Advance(5);
                    codeMatcherCursor.Instruction.WithLabels(isConstructionObstacleLabel);
                }
            }
        }

        static void ClearConstructionObstacles(List<GameObject> results)
        {
            results.RemoveAll(IsConstructionObstacle);
        }

        static void DefineDestroyMaterialIfNot()
        {
            if (destroyableObstacleMat == null)
            {
                destroyableObstacleMat = new Material(Builder.builderObstacleMaterial);
                destroyableObstacleMat.SetColor(ShaderPropertyID._Tint, Color.red);
            }
        }

        static bool IsConstructionObstacle(GameObject go)
        {
            return go.GetComponent<ConstructionObstacle>() != null;
        }
    }
}
