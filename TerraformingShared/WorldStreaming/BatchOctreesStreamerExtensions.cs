using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using WorldStreaming;

namespace Terraforming.WorldStreaming
{
    static class BatchOctreesStreamerExtensions
    {
        public static byte materialTypeOfLastOctreesEditAdd = 0;

        public static byte GetMaterialTypeOfLastOctreesEditAdd(this BatchOctreesStreamer batchOctreesStreamer)
        {
            return materialTypeOfLastOctreesEditAdd;
        }

        public static string GetTmpPath(this BatchOctreesStreamer batchOctreesStreamer, Int3 batchId)
        {
            var tmpPathPrefix = Path.Combine(LargeWorldStreamer.main.tmpPathPrefix, "CompiledOctreesCache");

            if (!Directory.Exists(tmpPathPrefix))
            {
                Directory.CreateDirectory(tmpPathPrefix);
            }

            var fileName = $"compiled-batch-{batchId.x}-{batchId.y}-{batchId.z}.optoctrees";
            var fullPath = Path.Combine(tmpPathPrefix, fileName);

            return fullPath;
        }

        public static void SetBatchOctree(this BatchOctreesStreamer batchOctreesStreamer, Int3 absoluteOctreeId, VoxelandData.OctNode root)
        {
            var numOctreesPerBatch = batchOctreesStreamer.numOctreesPerBatch;

            var batchId = Int3.FloorDiv(absoluteOctreeId, numOctreesPerBatch);
            var batch = batchOctreesStreamer.GetBatch(batchId);

            var octreeId = absoluteOctreeId - (batchId * numOctreesPerBatch);

            Logger.Debug($"numOctreesPerBatch = {numOctreesPerBatch}, batchId = {batchId}, octreeId = {octreeId}, absoluteOctreeId = {absoluteOctreeId}");

            batch.SetOctree(octreeId, root);
        }

        public static void WriteBatchOctrees(this BatchOctreesStreamer batchOctreesStreamer)
        {
            var batches = batchOctreesStreamer.batches;
            foreach (var batchOctrees in batches)
            {
                if (batchOctrees != null && batchOctrees.IsLoaded() && (batchOctrees.GetIsDirty()))
                {
                    Logger.Info($"Octrees of batch {batchOctrees.id} is dirty. Writing to temp save data prior saving to save slot.");
                    batchOctrees.WriteOctrees();
                }
            }
        }

        public static void PerformOctreesEdit(this BatchOctreesStreamer batchOctreesStreamer, Int3.Bounds blockBounds, LargeWorldStreamer.DistanceField df, bool isAdd = false, byte type = 1)
        {
            var args = new VoxelandData.OctNode.BlendArgs(isAdd ? VoxelandData.OctNode.BlendOp.Union : VoxelandData.OctNode.BlendOp.Subtraction, false, isAdd ? type : (byte)0);
            int blocksPerTree = LargeWorldStreamer.main.blocksPerTree;

            materialTypeOfLastOctreesEditAdd = 0;

            blockBounds = blockBounds.Expanded(1);
            foreach (Int3 @int in blockBounds / blocksPerTree)
            {
                if (LargeWorldStreamer.main.CheckRoot(@int))
                {
                    Octree octree = batchOctreesStreamer.GetOctree(@int);
                    if (octree != null)
                    {
                        Int3.Bounds bounds = @int.Refined(blocksPerTree);
                        VoxelandData.OctNode root = octree.ToVLOctree();
                        foreach (Int3 int2 in bounds.Intersect(blockBounds))
                        {
                            Vector3 wsPos = LargeWorldStreamer.main.land.transform.TransformPoint(int2 + UWE.Utils.half3);

                            float num = df(wsPos);
                            if (num >= 0f)
                            {
                                VoxelandData.OctNode i = new VoxelandData.OctNode(type, VoxelandData.OctNode.EncodeDensity(num));

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

                        batchOctreesStreamer.SetBatchOctree(@int, root);

                        root.Clear();
                    }
                }
            }
        }
    }
}
