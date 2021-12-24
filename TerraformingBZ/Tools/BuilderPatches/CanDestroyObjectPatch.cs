﻿using HarmonyLib;
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
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Callvirt, GetComponentMethod.MakeGenericMethod(typeof(ConstructionObstacle)));
                        yield return new CodeInstruction(OpCodes.Brtrue_S, onConstructionObstacleLabel);

                        if (instructionsEnumerator.MoveNext())
                        {
                            yield return instructionsEnumerator.Current.WithLabels(onDisabledDestroyingObstaclesLabel);
                        }

                        while (instructionsEnumerator.MoveNext() && !instructionsEnumerator.Current.labels.Contains(onNotIObstacleNullableLabel.Value))
                        {
                            yield return instructionsEnumerator.Current;
                        }

                        yield return instructionsEnumerator.Current.WithLabels(onConstructionObstacleLabel);
                    }
                }
                else
                {
                    yield return instructionsEnumerator.Current;
                }
            }
        }
    }
}