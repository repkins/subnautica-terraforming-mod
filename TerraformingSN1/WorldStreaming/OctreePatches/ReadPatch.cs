using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WorldStreaming;

namespace Terraforming.WorldStreaming.OctreePatches
{
    [HarmonyPatch(typeof(Octree))]
    [HarmonyPatch("Read")]
    static class ReadPatch
    {
        static void Postfix(Octree __instance, Int3 batchId)
        {

        }
    }
}
