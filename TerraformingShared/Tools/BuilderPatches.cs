using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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
                        yield return new CodeInstruction(OpCodes.Ldarg_0);                                  // Load passed "go" object to get ConstructionObstacle component.
                        yield return new CodeInstruction(OpCodes.Callvirt, GetComponentMethod.MakeGenericMethod(typeof(ConstructionObstacle)));
                        yield return new CodeInstruction(OpCodes.Brtrue_S, onConstructionObstacleLabel);    // Redirect if there is ConstructionObstacle component.

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
                // Exclude terrain obstacles so they gets terraformed.
                results.RemoveAll((gameObject) => Builder.IsObstacle(gameObject.GetComponent<Collider>()));
            }
        }
    }

    [HarmonyPatch(typeof(Builder))]
    [HarmonyPatch(nameof(Builder.UpdateAllowed))]
    static class UpdateAllowedPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codeMatcherCursor = new CodeMatcher(instructions);

            PatchClearFromConstructionObstacles(codeMatcherCursor, generator);
            if (codeMatcherCursor.IsInvalid)
            {
                codeMatcherCursor.ReportFailure(AccessTools.Method(typeof(Builder), nameof(Builder.UpdateAllowed)), Logger.Warning);
                return instructions;
            }

            return codeMatcherCursor.InstructionEnumeration();
        }

        static void PatchClearFromConstructionObstacles(CodeMatcher codeCursor, ILGenerator generator)
        {
#if BelowZero
            codeCursor.Start();
            codeCursor.MatchForward(false,
                new CodeMatch(OpCodes.Ldloc_1),
                new CodeMatch(OpCodes.Ldloc_3),     // Loads "obstaclesList" variable into evaluation stack to get count of obstacles.
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<GameObject>), nameof(List<GameObject>.Count))),
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(OpCodes.Ceq),
                new CodeMatch(OpCodes.And),
                new CodeMatch(OpCodes.Stloc_1)      // Assigns result of bitwise operation to "hasObstacles" bool.
            );
#else
            codeCursor.Start();
            codeCursor.MatchForward(false,
                new CodeMatch(OpCodes.Ldloc_S),     // Loads "obstaclesList" variable into evaluation stack to get count of obstacles.
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<GameObject>), nameof(List<GameObject>.Count))),
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(OpCodes.Ceq),
                new CodeMatch(OpCodes.Stloc_1)      // Assigns result of bitwise operation to "hasObstacles" bool.
            );
#endif

            if (codeCursor.IsValid)
            {
                var labels = codeCursor.Instruction.ExtractLabels();
#if !BelowZero
                var obstaclesListLocal = (LocalBuilder)codeCursor.Operand;
                codeCursor.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc_S, obstaclesListLocal).WithLabels(labels));
#else
                codeCursor.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc_3).WithLabels(labels));
#endif
                // Make it to allow construction by clearing contruction obstacles so "hasObstacles" would be false.
                codeCursor.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Call, AccessTools.Method($"{typeof(UpdateAllowedPatch)}:{nameof(ClearConstructionObstacles)}", new Type[] { typeof(List<GameObject>) }))
                );
            }
        }

        static void ClearConstructionObstacles(List<GameObject> results)
        {
            results.RemoveAll(IsConstructionObstacle);
#if !BelowZero
            results.RemoveAll(IsImmuneToPropulsion);
#endif
        }

        static bool IsConstructionObstacle(GameObject go)
        {
            return go.GetComponent<ConstructionObstacle>() != null;
        }

        static bool IsImmuneToPropulsion(GameObject go)
        {
            return go.GetComponent<ImmuneToPropulsioncannon>() != null;
        }
    }
}
