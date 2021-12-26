using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

#if BelowZero
using UnityEngine.Rendering;
#endif

namespace Terraforming.Tools.BuilderPatches
{
    [HarmonyPatch(typeof(Builder))]
    [HarmonyPatch(nameof(Builder.CanDestroyObject))]
    static class CanDestroyObjectPatch
    {
        private static MethodInfo GetComponentMethod = typeof(GameObject).GetMethod(nameof(GameObject.GetComponent), Type.EmptyTypes);

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsEnumerator = instructions.GetEnumerator();
            while (instructionsEnumerator.MoveNext())
            {
                if (instructionsEnumerator.Current.Calls(GetComponentMethod.MakeGenericMethod(typeof(IObstacle))))
                {
                    yield return instructionsEnumerator.Current;

                    if (instructionsEnumerator.MoveNext() && instructionsEnumerator.Current.Branches(out var onNotIObstacleNullableLabel))
                    {
                        yield return instructionsEnumerator.Current;

                        var onDisabledDestroyingObstaclesLabel = generator.DefineLabel();
                        yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Config), nameof(Config.Instance)));
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Config), nameof(Config.destroyLargerObstaclesOnConstruction)));
                        yield return new CodeInstruction(OpCodes.Brfalse_S, onDisabledDestroyingObstaclesLabel);

                        var onConstructionObstacleLabel = generator.DefineLabel();
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Callvirt, GetComponentMethod.MakeGenericMethod(typeof(ConstructionObstacle)));
                        yield return new CodeInstruction(OpCodes.Brtrue_S, onConstructionObstacleLabel);

#if !BelowZero
                        var onImmuneToLabel = generator.DefineLabel();
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Callvirt, GetComponentMethod.MakeGenericMethod(typeof(ImmuneToPropulsioncannon)));
                        yield return new CodeInstruction(OpCodes.Brtrue_S, onImmuneToLabel);
#endif

                        if (instructionsEnumerator.MoveNext())
                        {
                            yield return instructionsEnumerator.Current.WithLabels(onDisabledDestroyingObstaclesLabel);
                        }

                        while (instructionsEnumerator.MoveNext() && !instructionsEnumerator.Current.labels.Contains(onNotIObstacleNullableLabel.Value))
                        {
                            yield return instructionsEnumerator.Current;
                        }

#if BelowZero
                        yield return instructionsEnumerator.Current.WithLabels(onConstructionObstacleLabel);
#else
                        yield return instructionsEnumerator.Current.WithLabels(onConstructionObstacleLabel, onImmuneToLabel);
#endif
                    }
                }
                else
                {
                    yield return instructionsEnumerator.Current;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Builder))]
    [HarmonyPatch("GetObstacles")]
    static class GetObstaclesPatch
    {
        static void Postfix(Vector3 position, Quaternion rotation, List<OrientedBounds> localBounds, List<GameObject> results)
        {
            if (Config.Instance.habitantModulesPartialBurying)
            {
                results.RemoveAll((gameObject) => Builder.IsObstacle(gameObject.GetComponent<Collider>()));
            }

#if !BelowZero
            if (Config.Instance.destroyLargerObstaclesOnConstruction)
            {
                results.RemoveAll((gameObject) => gameObject.GetComponent<ConstructionObstacle>() != null);
                results.RemoveAll((gameObject) => gameObject.GetComponent<ImmuneToPropulsioncannon>() != null);
            }
#endif
        }
    }

#if BelowZero
    [HarmonyPatch(typeof(Builder))]
    [HarmonyPatch(nameof(Builder.UpdateAllowed))]
    static class UpdateAllowedPatch
    {
        static Material destroyableObstacleMat = null;

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codeMatcherCursor = new CodeMatcher(instructions);

            PatchClearFromConstructionObstacles(codeMatcherCursor, generator);
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

        static void PatchClearFromConstructionObstacles(CodeMatcher codeCursor, ILGenerator generator)
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
                // Remove boolean result assignent as we are "moving" it to after obstacle highlighting block
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

                        // Make it to allow construction by clearing contruction obstacles for boolean result assignment if destroying obstacles is enabled
                        var disabledDestroyObstaclesLabel = generator.DefineLabel();
                        codeCursor.InsertAndAdvance(
                            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Config), nameof(Config.Instance))).WithLabels(labels),
                            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Config), nameof(Config.destroyLargerObstaclesOnConstruction))),
                            new CodeInstruction(OpCodes.Brfalse_S, disabledDestroyObstaclesLabel)
                        );

                        codeCursor.InsertAndAdvance(
                            new CodeInstruction(OpCodes.Ldloc_3),
                            new CodeInstruction(OpCodes.Call, AccessTools.Method($"{typeof(UpdateAllowedPatch)}:{nameof(ClearConstructionObstacles)}", new Type[] { typeof(List<GameObject>) }))
                        );

                        // Assign boolean result here (was before highlighting block)
                        codeCursor.InsertAndAdvance(
                            new CodeInstruction(OpCodes.Ldloc_1).WithLabels(disabledDestroyObstaclesLabel),
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

                    // Check if obstacle destroying is enabled
                    var disabledDestroyObstaclesLabel = generator.DefineLabel();
                    codeMatcherCursor
                        .InsertAndAdvance(
                            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Config), nameof(Config.Instance))),
                            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Config), nameof(Config.destroyLargerObstaclesOnConstruction))),
                            new CodeInstruction(OpCodes.Brfalse_S, disabledDestroyObstaclesLabel)
                        );

                    // Then check if construction obstacle
                    var notConstructionObstacleLabel = generator.DefineLabel();
                    codeMatcherCursor
                        .InsertAndAdvance(
                            new CodeInstruction(OpCodes.Ldloc_S, obstacleLocal),
                            new CodeInstruction(OpCodes.Call, AccessTools.Method($"{typeof(UpdateAllowedPatch)}:{nameof(IsConstructionObstacle)}", new Type[] { typeof(GameObject) })),
                            new CodeInstruction(OpCodes.Brfalse_S, notConstructionObstacleLabel)
                        );

                    // Then define destroy material
                    codeMatcherCursor
                        .InsertAndAdvance(
                            new CodeInstruction(OpCodes.Call, AccessTools.Method($"{typeof(UpdateAllowedPatch)}:{nameof(DefineDestroyMaterialIfNot)}"))
                        );

                    // And then highlight in red
                    codeMatcherCursor
                        .InsertAndAdvance(
                            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Builder), nameof(Builder.obstaclesBuffer))),
                            new CodeInstruction(OpCodes.Ldloc_S, rendererLocal), // Renderer
                            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(UpdateAllowedPatch), nameof(destroyableObstacleMat))),
                            new CodeInstruction(OpCodes.Ldloc_S, submeshIndexLocal), // int
                            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(CommandBuffer), nameof(CommandBuffer.DrawRenderer), new Type[] { typeof(Renderer), typeof(Material), typeof(int) }))
                        );

                    // Then jump over instructions within "else" block
                    var isConstructionObstacleLabel = generator.DefineLabel();
                    codeMatcherCursor
                        .InsertAndAdvance(
                            new CodeInstruction(OpCodes.Br_S, isConstructionObstacleLabel)
                        );

                    // Or else highlight in default (yellow)
                    codeMatcherCursor.Instruction.WithLabels(disabledDestroyObstaclesLabel, notConstructionObstacleLabel);
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
#endif
}
