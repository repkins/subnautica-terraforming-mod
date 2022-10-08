using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WorldStreaming;

namespace Terraforming.WorldStreaming.ClipmapCellPatches
{
#if !BelowZero
    [HarmonyPatch(typeof(ClipmapCell))]
    [HarmonyPatch("BeginBuildLayers")]
    [HarmonyPatch(new Type[] { typeof(MeshBuilder) })]
    static class BeginBuildLayersPatch
    {
        static void Prefix(out ClipmapChunk __state, ClipmapChunk ___chunk)
        {
            var oldChunk = ___chunk;
            __state = oldChunk;

            if (oldChunk && oldChunk.gameObject)
            {
                UnityEngine.Object.Destroy(oldChunk.gameObject);
            }
        }
    }
#endif
}
