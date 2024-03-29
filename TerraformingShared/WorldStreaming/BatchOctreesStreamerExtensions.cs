﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using WorldStreaming;

namespace Terraforming.WorldStreaming
{
    static class BatchOctreesStreamerExtensions
    {
        public const string CompiledOctreesDirName = "CompiledOctreesCache";
#if BelowZero
        public const string CompiledOctreesFileNamePrefix = "compiled-batch";
#else
        public const string CompiledOctreesFileNamePrefix = "batch-compiled";
#endif

        public static string GetTmpPath(this BatchOctreesStreamer batchOctreesStreamer, Int3 batchId)
        {
            var tmpPathPrefix = Path.Combine(LargeWorldStreamer.main.tmpPathPrefix, CompiledOctreesDirName);

            if (!Directory.Exists(tmpPathPrefix))
            {
                Directory.CreateDirectory(tmpPathPrefix);
            }

            var fileName = $"{CompiledOctreesFileNamePrefix}-{batchId.x}-{batchId.y}-{batchId.z}.optoctrees";
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
    }
}
