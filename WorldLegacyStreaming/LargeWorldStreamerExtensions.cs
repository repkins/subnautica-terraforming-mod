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
		public static void AddToOctreesEdit(this LargeWorldStreamer largeWorldStreamer, Int3.Bounds localBlockBounds, LargeWorldStreamer.DistanceField df, bool isAdd = false, byte type = 1)
		{
			localBlockBounds = localBlockBounds.Expanded(1);
			var octreesEditData = new OctreesEditData(localBlockBounds, isAdd, type, df);

			largeWorldStreamer.PerformOctreesEditThreaded(octreesEditData);
		}

		public static void PerformOctreesEditThreaded(this LargeWorldStreamer largeWorldStreamer, OctreesEditData octreesEditData)
		{
			var streamerV2 = largeWorldStreamer.streamerV2;

			foreach (Int3 octreeId in octreesEditData.localBlockBounds / largeWorldStreamer.blocksPerTree)
			{
				if (largeWorldStreamer.CheckRoot(octreeId))
				{
					Octree octree = streamerV2.octreesStreamer.GetOctree(octreeId);
					if (octree != null)
					{
						Int3.Bounds bounds = octreeId.Refined(largeWorldStreamer.blocksPerTree);
						VoxelandData.OctNode rootNode = octree.ToVLOctree();
						foreach (Int3 int2 in bounds.Intersect(octreesEditData.localBlockBounds))
						{
							Vector3 wsPos = octreesEditData.localToWorldMatrix.MultiplyPoint3x4(int2 + UWE.Utils.half3);
							float num = octreesEditData.df(wsPos);
							VoxelandData.OctNode i = new VoxelandData.OctNode((num >= 0f) ? octreesEditData.type : (byte)0, VoxelandData.OctNode.EncodeDensity(num));
							int blocksPerTree = largeWorldStreamer.blocksPerTree;
							int x = int2.x % blocksPerTree;
							int y = int2.y % blocksPerTree;
							int z = int2.z % blocksPerTree;
							VoxelandData.OctNode octNode = VoxelandData.OctNode.Blend(rootNode.GetNode(x, y, z, blocksPerTree / 2), i, octreesEditData.blendArgs);
							rootNode.SetNode(x, y, z, blocksPerTree / 2, octNode.type, octNode.density);
						}
						rootNode.Collapse();

						streamerV2.octreesStreamer.SetBatchOctree(octreeId, rootNode);

						rootNode.Clear();
					}
				}
			}

			streamerV2.clipmapStreamer.AddToRangesEdited(octreesEditData.localBlockBounds);
		}
	}
}
