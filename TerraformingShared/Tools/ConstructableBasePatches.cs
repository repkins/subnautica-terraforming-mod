using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraforming.WorldLegacyStreaming;
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
            var newIsConstructed = value;

            if (Config.Instance.habitantModulesPartialBurying)
            {
                if (__instance._constructed != newIsConstructed && newIsConstructed)
                {
                    var constructableBoundsList = new List<ConstructableBounds>();
                    __instance.GetComponentsInChildren(true, constructableBoundsList);

                    var hasAnyOverlappedTerrainObstacles = false;

                    var constructableWorldBoundsList = constructableBoundsList.Select(constructableBounds => OrientedBounds.ToWorldBounds(constructableBounds.transform, constructableBounds.bounds));
                    foreach (var constructableWorldBounds in constructableWorldBoundsList)
                    {
                        Logger.Debug($"Checking constructable world bounds: {constructableWorldBounds}");

                        var overlappedObjects = new List<GameObject>();
                        Builder.GetOverlappedObjects(constructableWorldBounds.position, constructableWorldBounds.rotation, constructableWorldBounds.extents, overlappedObjects);

                        if (overlappedObjects.Any((gameObject) => Builder.IsObstacle(gameObject.GetComponent<Collider>())))
                        {
                            hasAnyOverlappedTerrainObstacles = true;
                            break;
                        }
                    }

                    if (hasAnyOverlappedTerrainObstacles)
                    {
                        LargeWorldStreamer.main.PerformBoxesEdit(constructableWorldBoundsList, false, 2);
                    }
                }
            }
        }
    }
}
