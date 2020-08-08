using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraforming.WorldStreaming;
using UnityEngine;

namespace Terraforming.Tools.TerraformerPatches
{
    [HarmonyPatch(typeof(Terraformer))]
    [HarmonyPatch("Update")]
    static class UpdatePatch
    {
        static bool Prefix(Terraformer __instance, out bool __state, bool ___penDown, Stack<GameObject> ___activeStrokes)
        {
            __state = false;

            if (ClipmapLevelExtensions.isMeshesRebuilding)
            {
                return false;
            }
            else
            {
                var probe = __instance.GetProbe();
                if (probe && !probe.activeSelf)
                {
                    probe.SetActive(true);
                }
            }

            if (LargeWorld.main == null)
            {
                return false;
            }
            if (__instance.GetUsingPlayer() == null)
            {
                return false;
            }

            var hasActiveStrokes = ___activeStrokes.Count > 0;
            var isAnyHandHeld = __instance.GetUsingPlayer().GetRightHandHeld() || __instance.GetUsingPlayer().GetLeftHandHeld();
            if (___penDown && hasActiveStrokes && !isAnyHandHeld)
            {
                __state = true;
            }

            return true;
        }

        static void Postfix(Terraformer __instance, bool __state)
        {
            if (__state)
            {
                var streamerV2 = LargeWorldStreamer.main.streamerV2;
                streamerV2.clipmapStreamer.FlushRangesEdited(streamerV2.octreesStreamer.minLod, streamerV2.octreesStreamer.maxLod);

                var probe = __instance.GetProbe();
                if (probe)
                {
                    probe.SetActive(false);
                }
            }
        }
    }
}
