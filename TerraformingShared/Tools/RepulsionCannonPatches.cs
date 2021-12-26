using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraforming.WorldStreaming;
using UnityEngine;
using WorldStreaming;

namespace Terraforming.Tools.RepulsionCannonPatches
{
    [HarmonyPatch(typeof(RepulsionCannon))]
    [HarmonyPatch("OnToolUseAnim")]
    static class OnToolUseAnimPatch
    {
        static void Prefix(RepulsionCannon __instance)
        {
            if (Config.Instance.terrainImpactWithPropulsionCannon && !ClipmapLevelExtensions.isMeshesRebuilding)
            {
                var energyMixin = __instance.GetEnergyMixin();
                if (energyMixin.charge > 0f)
                {
                    GameObject closestObject = null;
                    var closestPoint = default(Vector3);

                    if (UWE.Utils.TraceFPSTargetPosition(Player.main.gameObject, 35f, ref closestObject, ref closestPoint, false))
                    {
                        if (closestObject && closestObject.GetComponent<TerrainChunkPiece>() != null)
                        {
                            Utils.Terraform(closestPoint, 1f);
                        }
                    }
                }
            }
        }
    }
}
