using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraforming.WorldStreaming;

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
}
