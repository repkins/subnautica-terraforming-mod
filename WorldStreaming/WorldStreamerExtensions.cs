using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WorldStreaming;

namespace Terraforming.WorldStreaming
{
    static class WorldStreamerExtensions
    {
		public static void AddToWorldEdit(this WorldStreamer worldStreamer, Int3.Bounds localBlockBounds, LargeWorldStreamer.DistanceField df, bool isAdd = false, byte type = 1)
		{
			localBlockBounds = localBlockBounds.Expanded(1);
			var octreesEditData = new OctreesEditData(localBlockBounds, isAdd, type, df);

			worldStreamer.PerformWorldEditThreaded(octreesEditData);
		}

		public static void PerformWorldEditThreaded(this WorldStreamer worldStreamer, OctreesEditData octreesEditData)
		{
			worldStreamer.octreesStreamer.PerformOctreesEditThreaded(octreesEditData);

			worldStreamer.clipmapStreamer.AddToRangesEdited(octreesEditData.localBlockBounds);
		}
	}
}
