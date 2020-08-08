using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WorldStreaming;

namespace Terraforming.WorldStreaming.BatchOctreesStreamerPatches
{
    [HarmonyPatch(typeof(BatchOctreesStreamer))]
    [HarmonyPatch("GetPath")]
    static class GetPathPatch
    {
        static bool Prefix(BatchOctreesStreamer __instance, Int3 batchId, ref string __result)
        {
            var tmpPath = __instance.GetTmpPath(batchId);

            if (File.Exists(tmpPath))
            {
                Logger.Info($"{tmpPath} exists");
                __result = tmpPath;

                return false;
            } 

            return true;
        }
    }
}
