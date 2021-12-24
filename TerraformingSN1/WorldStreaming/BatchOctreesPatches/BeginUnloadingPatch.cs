using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using WorldStreaming;

namespace Terraforming.WorldStreaming.BatchOctreesPatches
{
    [HarmonyPatch(typeof(BatchOctrees))]
    [HarmonyPatch("BeginUnloading")]
    static class BeginUnloadingPatch
    {
        static void Prefix(BatchOctrees __instance)
        {
            if (__instance.GetIsDirty())
            {
                Logger.Info($"Octrees of batch {__instance.id} is dirty. Writing to temp save data prior unloading");
                __instance.WriteOctrees();
            }
        }
    }
}
