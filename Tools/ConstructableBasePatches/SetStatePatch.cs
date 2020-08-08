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

                    var hasAnyOverlappedObstacles = false;
                    foreach (var constructableBounds in constructableBoundsList)
                    {
                        OrientedBounds orientedBounds = OrientedBounds.ToWorldBounds(constructableBounds.transform, constructableBounds.bounds);

                        var overlappedObjects = new List<GameObject>();
                        Builder.GetOverlappedObjects(orientedBounds.position, orientedBounds.rotation, orientedBounds.extents, overlappedObjects);

                        if (overlappedObjects.Any((gameObject) => Builder.IsObstacle(gameObject.GetComponent<Collider>())))
                        {
                            LargeWorldStreamer.main.PerformBoxEdit(new Bounds(orientedBounds.position, orientedBounds.size + new Vector3(1f, 1f, 1f)), orientedBounds.rotation, false, 2);
                            Logger.Debug($"PerformBoxEdit() called using oriented bounds: {orientedBounds}");
                            hasAnyOverlappedObstacles = true;
                        }
                    }

                    if (hasAnyOverlappedObstacles)
                    {
                        var streamerV2 = LargeWorldStreamer.main.streamerV2;
                        streamerV2.clipmapStreamer.FlushRangesEdited(streamerV2.octreesStreamer.minLod, streamerV2.octreesStreamer.maxLod);
                    }
                }
            }
        }
    }
}
