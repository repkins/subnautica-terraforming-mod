using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using Terraforming.WorldStreaming;
using UnityEngine;

namespace Terraforming.Tools.ConstructableBasePatches
{
    [HarmonyPatch(typeof(ConstructableBase))]
    [HarmonyPatch(nameof(ConstructableBase.SetState))]
    static class SetStatePatch
    {
        [HarmonyPrefix]
        static void Prefix(ConstructableBase __instance, bool value)
        {
            if (Config.Instance.habitantModulesPartialBurying)
            {
                if (__instance._constructed != value && value)
                {
                    var constructableBoundsList = new List<ConstructableBounds>();
                    __instance.GetComponentsInChildren(true, constructableBoundsList);

                    var hasAnyOverlappedTerrainObstacles = false;

                    var orientedBoundsList = constructableBoundsList.Select(constructableBounds => OrientedBounds.ToWorldBounds(constructableBounds.transform, constructableBounds.bounds));
                    foreach (var orientedBounds in orientedBoundsList)
                    {
                        Logger.Debug($"Checking oriented bounds: {orientedBounds}");

                        var overlappedObjects = new List<GameObject>();
                        Builder.GetOverlappedObjects(orientedBounds.position, orientedBounds.rotation, orientedBounds.extents, overlappedObjects);

                        if (overlappedObjects.Any((gameObject) => Builder.IsObstacle(gameObject.GetComponent<Collider>())))
                        {
                            hasAnyOverlappedTerrainObstacles = true;
                            break;
                        }
                    }

                    if (hasAnyOverlappedTerrainObstacles)
                    {
                        foreach (var orientedBounds in orientedBoundsList)
                        {
                            var sizeExpand = Config.Instance.spaceBetweenTerrainHabitantModule;
#if BelowZero
                            byte matType = 1;
#else
                            byte matType = 2;
#endif
                            LargeWorldStreamer.main.PerformBoxEdit(new Bounds(orientedBounds.position, orientedBounds.size + new Vector3(sizeExpand, sizeExpand, sizeExpand)), orientedBounds.rotation, false, matType);
                            Logger.Debug($"PerformBoxEdit() called using oriented bounds: {orientedBounds}");
                        }

                        var streamerV2 = LargeWorldStreamer.main.streamerV2;
                        streamerV2.clipmapStreamer.FlushRangesEdited(streamerV2.octreesStreamer.minLod, streamerV2.octreesStreamer.maxLod);
                    }
                }
            }
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codeMatcherCursor = new CodeMatcher(instructions);

            PatchDestroyConstructionObstacles(codeMatcherCursor, generator);
            if (codeMatcherCursor.IsInvalid)
            {
                codeMatcherCursor.ReportFailure(AccessTools.Method(typeof(ConstructableBase), nameof(ConstructableBase.SetState)), Logger.Warning);
                return instructions;
            }

            return codeMatcherCursor.InstructionEnumeration();
        }

        static void PatchDestroyConstructionObstacles(CodeMatcher codeCursor, ILGenerator generator)
        {
            // Find constructed state comparison with incoming.
            codeCursor.Start();
            codeCursor.MatchForward(useEnd: true,
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Constructable), nameof(Constructable._constructed))),
                new CodeMatch(OpCodes.Ldarg_1),
                new CodeMatch(OpCodes.Ceq)
            );

            if (codeCursor.IsValid)
            {
                // Remove incoming constructed state true check.
                codeCursor.Advance(1);
                codeCursor.RemoveInstructions(4);
                codeCursor.SetOpcodeAndAdvance(OpCodes.Brtrue);
            }

            // Find BaseGhost.Finish() call.
            codeCursor.Start();
            codeCursor.MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Constructable), nameof(Constructable.model))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(GameObject), nameof(GameObject.GetComponent), null, new[] { typeof(BaseGhost) } )),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(BaseGhost), nameof(BaseGhost.Finish)))
            );

            if (codeCursor.IsValid)
            {
                var labels = codeCursor.Instruction.ExtractLabels();

                // Wrap under incoming constructed state true condition.
                var notConstructedLabel = generator.DefineLabel();
                codeCursor.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_1).WithLabels(labels),
                    new CodeInstruction(OpCodes.Ldc_I4_1),
                    new CodeInstruction(OpCodes.Ceq),
                    new CodeInstruction(OpCodes.Brfalse, notConstructedLabel)
                );

                codeCursor.Advance(4);
                codeCursor.Instruction.WithLabels(notConstructedLabel);
            }

            // Find UnityEngine.Object.Destroy() call.
            codeCursor.Start();
            codeCursor.MatchForward(false,
                new CodeMatch(OpCodes.Call, AccessTools.Method($"{typeof(UnityEngine.Object)}:{nameof(UnityEngine.Object.Destroy)}", new[] { typeof(GameObject) }))
            );

            if (codeCursor.IsValid)
            {
                // Replace destroy call with show-hide call.
                codeCursor.InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_1));
                codeCursor.SetOperandAndAdvance(AccessTools.Method($"{typeof(SetStatePatch)}:{nameof(ShowHideConstructionObstacle)}", new[] { typeof(GameObject), typeof(bool) }));
            }
        }

        static void ShowHideConstructionObstacle(GameObject gameObject, bool isConstructed)
        {
            var visible = isConstructed ? false : true;

            gameObject.SetActive(visible);
        }
    }
}
