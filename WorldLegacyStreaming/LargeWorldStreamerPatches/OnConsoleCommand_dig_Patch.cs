using Harmony;
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
        static bool Prefix()
        {
            if (WorldStreamerExtensions.isOctreesEditing || ClipmapLevelExtensions.isMeshesRebuilding)
            {
                return false;
            }

            return true;
        }

        static void Postfix()
        {
            var streamerV2 = LargeWorldStreamer.main.streamerV2;
            streamerV2.FlushWorldEdit();
        }
    }
}
