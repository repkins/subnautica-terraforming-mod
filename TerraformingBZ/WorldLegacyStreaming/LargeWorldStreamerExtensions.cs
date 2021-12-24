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
        private static byte materialTypeOfLastOctreesEditAdd = 0;

		public static byte GetMaterialTypeOfLastOctreesEditAdd(this LargeWorldStreamer largeWorldStreamer)
        {
            return materialTypeOfLastOctreesEditAdd;
        }

        public static void PerformOctreesEdit(this LargeWorldStreamer largeWorldStreamer, Int3.Bounds blockBounds, LargeWorldStreamer.DistanceField df, bool isAdd = false, byte type = 1)
		{
			VoxelandData.OctNode.BlendArgs args = new VoxelandData.OctNode.BlendArgs(isAdd ? VoxelandData.OctNode.BlendOp.Union : VoxelandData.OctNode.BlendOp.Subtraction, false, isAdd ? type : (byte)0);
			var streamerV2 = largeWorldStreamer.streamerV2;

			materialTypeOfLastOctreesEditAdd = 0;

			blockBounds = blockBounds.Expanded(1);
			foreach (Int3 @int in blockBounds / largeWorldStreamer.blocksPerTree)
			{
				if (largeWorldStreamer.CheckRoot(@int))
				{
					Octree octree = streamerV2.octreesStreamer.GetOctree(@int);
					if (octree != null)
					{
						Int3.Bounds bounds = @int.Refined(largeWorldStreamer.blocksPerTree);
						VoxelandData.OctNode root = octree.ToVLOctree();
						foreach (Int3 int2 in bounds.Intersect(blockBounds))
						{
							Vector3 wsPos = largeWorldStreamer.land.transform.TransformPoint(int2 + UWE.Utils.half3);

							float num = df(wsPos);
							if (num >= 0f)
                            {
								VoxelandData.OctNode i = new VoxelandData.OctNode(type, VoxelandData.OctNode.EncodeDensity(num));

                                int blocksPerTree = largeWorldStreamer.blocksPerTree;
                                int x = int2.x % blocksPerTree;
                                int y = int2.y % blocksPerTree;
                                int z = int2.z % blocksPerTree;

								var node = root.GetNode(x, y, z, blocksPerTree / 2);
								if (!isAdd && materialTypeOfLastOctreesEditAdd <= node.type)
                                {
									materialTypeOfLastOctreesEditAdd = node.type;
                                }

                                VoxelandData.OctNode octNode = VoxelandData.OctNode.Blend(node, i, args);
                                root.SetNode(x, y, z, blocksPerTree / 2, octNode.type, octNode.density);
							}
						}
						root.Collapse();

						streamerV2.octreesStreamer.SetBatchOctree(@int, root);

						root.Clear();
					}
				}
			}

			streamerV2.clipmapStreamer.AddToRangesEdited(blockBounds);
		}
	}
}
