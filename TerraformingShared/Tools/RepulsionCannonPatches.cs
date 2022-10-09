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
            if (Config.Instance.terrainImpactWithRepulsionCannon && !ClipmapLevelExtensions.isMeshesRebuilding)
            {
                var energyMixin = __instance.GetEnergyMixin();
                if (energyMixin.charge > 0f)
                {
#if BelowZero
                    GameObject closestObject = null;
                    var closestPoint = default(Vector3);

                    if (UWE.Utils.TraceFPSTargetPosition(Player.main.gameObject, 35f, ref closestObject, ref closestPoint, false))
                    {
                        if (closestObject && closestObject.GetComponent<TerrainChunkPiece>() != null)
                        {
                            Utils.Terraform(closestPoint, 1f);
                        }
                    }
#else
                    if (UWE.Utils.TraceHitComponentNormal<ClipmapChunk>(Player.main.gameObject, 35f, 1f, out _, out var position2))
                    {
                        Utils.Terraform(position2, 1f);
                    }
#endif
                }
            }
        }
    }
}
