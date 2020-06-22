using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Terraforming.WorldLegacyStreaming.LargeWorldStreamerPatches
{
    [HarmonyPatch(typeof(LargeWorldStreamer))]
    [HarmonyPatch("PerformVoxelEdit")]
    [HarmonyPatch(new Type[] { typeof(Int3.Bounds), typeof(LargeWorldStreamer.DistanceField), typeof(bool), typeof(byte) })]
    static class PerformVoxelEditPatch
    {
        static bool Prefix(LargeWorldStreamer __instance, Int3.Bounds blockBounds, LargeWorldStreamer.DistanceField df, bool isAdd = false, byte type = 1)
        {
            __instance.PerformOctreesEdit(blockBounds, df, isAdd, type);

            return false;
        }
    }
}
