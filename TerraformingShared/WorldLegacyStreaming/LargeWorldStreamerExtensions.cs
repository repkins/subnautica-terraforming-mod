using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraforming.WorldStreaming;
using UnityEngine;
using WorldStreaming;

namespace Terraforming.WorldLegacyStreaming
{
    static class LargeWorldStreamerExtensions
    {
		public static byte GetMaterialTypeOfLastOctreesEditAdd(this LargeWorldStreamer largeWorldStreamer)
        {
            return largeWorldStreamer.streamerV2.octreesStreamer.GetMaterialTypeOfLastOctreesEditAdd();
        }

        public static void PerformBoxesEdit(this LargeWorldStreamer largeWorldStreamer, IEnumerable<OrientedBounds> orientedBoundsList, bool isAdd = false, byte type = 1)
        {
            var sizeExpand = Config.Instance.spaceBetweenTerrainHabitantModule;

            foreach (var orientedBounds in orientedBoundsList)
            {
                // Perform only octrees edit
                LargeWorldStreamer.main.PerformBoxEdit(new Bounds(orientedBounds.position, orientedBounds.size + new Vector3(sizeExpand, sizeExpand, sizeExpand)), orientedBounds.rotation, isAdd, type);
                Logger.Debug($"PerformBoxEdit() called using oriented bounds: {orientedBounds}");
            }

            var streamerV2 = largeWorldStreamer.streamerV2;
            streamerV2.clipmapStreamer.FlushRangesEdited(streamerV2.octreesStreamer.minLod, streamerV2.octreesStreamer.maxLod);
        }
    }
}
