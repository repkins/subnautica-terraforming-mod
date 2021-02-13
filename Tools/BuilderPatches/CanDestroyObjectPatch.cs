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
            if (Config.Instance.destroyLargerObstaclesOnConstruction)
            {
                var instructionsEnumerator = instructions.GetEnumerator();
                while (instructionsEnumerator.MoveNext())
                {
                    if (instructionsEnumerator.Current.Calls(GetComponentMethod.MakeGenericMethod(typeof(IObstacle))))
                    {
                        yield return instructionsEnumerator.Current;

                        if (instructionsEnumerator.MoveNext() && instructionsEnumerator.Current.Branches(out var nullableNextFromOriginalLabel))
                        {
                            yield return instructionsEnumerator.Current;

                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Callvirt, GetComponentMethod.MakeGenericMethod(typeof(ConstructionObstacle)));

                            var nextFromInsertedLabel = generator.DefineLabel();
                            yield return new CodeInstruction(OpCodes.Brtrue_S, nextFromInsertedLabel);

                            while (instructionsEnumerator.MoveNext() && !instructionsEnumerator.Current.labels.Contains(nullableNextFromOriginalLabel.Value))
                            {
                                yield return instructionsEnumerator.Current;
                            }

                            yield return instructionsEnumerator.Current.WithLabels(nextFromInsertedLabel);
                        }
                    }
                    else
                    {
                        yield return instructionsEnumerator.Current;
                    }
                }
            }
            else
            {
                foreach (var instruction in instructions)
                {
                    yield return instruction;
                }
            }
        }
    }
}
