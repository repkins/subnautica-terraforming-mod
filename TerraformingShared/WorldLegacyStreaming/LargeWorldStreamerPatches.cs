using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraforming.WorldStreaming;
using UnityEngine;

namespace Terraforming.WorldLegacyStreaming.LargeWorldStreamerPatches
{
    [HarmonyPatch(typeof(LargeWorldStreamer))]
    [HarmonyPatch("OnConsoleCommand_dig")]
    static class OnConsoleCommand_dig_Patch
    {
        static void Postfix()
        {
            var streamerV2 = LargeWorldStreamer.main.streamerV2;
            streamerV2.clipmapStreamer.FlushRangesEdited(streamerV2.octreesStreamer.minLod, streamerV2.octreesStreamer.maxLod);
        }
    }

    [HarmonyPatch(typeof(LargeWorldStreamer))]
    [HarmonyPatch("PerformBoxEdit")]
    [HarmonyPatch(new Type[] { typeof(Bounds), typeof(Quaternion), typeof(bool), typeof(byte) })]
    static class PerformBoxEditPatch
    {
        static bool Prefix(LargeWorldStreamer __instance, Bounds bb, Quaternion rot, bool isAdd = false, byte type = 1)
        {
            Bounds aaBB = bb;

            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            OrientedBounds.MinMaxBounds(OrientedBounds.TransformMatrix(bb.center, rot), Vector3.zero, bb.extents, ref min, ref max);

            aaBB.SetMinMax(min, max);

            Quaternion invRot = Quaternion.Inverse(rot);
            Vector3 c = bb.center;

            __instance.PerformVoxelEdit(aaBB, (Vector3 wsPos) => VoxelandMisc.SignedDistToBox(bb, c + invRot * (wsPos - c)), isAdd, type);

            return false;
        }
    }

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
