using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Terraforming.Tools.BuilderPatches 
{
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

            if (Config.Instance.destroyLargerObstaclesOnConstruction)
            {
                results.RemoveAll((gameObject) => gameObject.GetComponent<ConstructionObstacle>() != null);
                results.RemoveAll((gameObject) => gameObject.GetComponent<ImmuneToPropulsioncannon>() != null);
            }
        }
    }
}
