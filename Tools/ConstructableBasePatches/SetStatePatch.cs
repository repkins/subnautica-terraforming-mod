using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraforming.WorldStreaming;
using UnityEngine;

namespace Terraforming.Tools.ConstructableBasePatches
{
    [HarmonyPatch(typeof(ConstructableBase))]
    [HarmonyPatch("SetState")]
    static class SetStatePatch
    {
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
                            LargeWorldStreamer.main.PerformBoxEdit(new Bounds(orientedBounds.position, orientedBounds.size + new Vector3(sizeExpand, sizeExpand, sizeExpand)), orientedBounds.rotation, false, 1);
                            Logger.Debug($"PerformBoxEdit() called using oriented bounds: {orientedBounds}");
                        }

                        var streamerV2 = LargeWorldStreamer.main.streamerV2;
                        streamerV2.clipmapStreamer.FlushRangesEdited(streamerV2.octreesStreamer.minLod, streamerV2.octreesStreamer.maxLod);
                    }
                }
            }
        }
    }
}
