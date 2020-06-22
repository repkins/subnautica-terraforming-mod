using System;
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
        private static readonly FieldInfo batchesField = typeof(BatchOctreesStreamer).GetField("batches", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo numOctreesPerBatchField = typeof(BatchOctreesStreamer).GetField("numOctreesPerBatch", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

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
            var numOctreesPerBatch = (int)numOctreesPerBatchField.GetValue(batchOctreesStreamer);

            var batchId = Int3.FloorDiv(absoluteOctreeId, numOctreesPerBatch);
            var batch = batchOctreesStreamer.GetBatch(batchId);

            var octreeId = absoluteOctreeId - (batchId * numOctreesPerBatch);

            Logger.Debug($"numOctreesPerBatch = {numOctreesPerBatch}, batchId = {batchId}, octreeId = {octreeId}, absoluteOctreeId = {absoluteOctreeId}");

            batch.SetOctree(octreeId, root);
        }

        public static void WriteBatchOctrees(this BatchOctreesStreamer batchOctreesStreamer)
        {
            var batches = batchesField.GetValue(batchOctreesStreamer) as Array3<BatchOctrees>;
            foreach (var batchOctrees in batches)
            {
                if (batchOctrees != null && batchOctrees.IsLoaded() && batchOctrees.GetIsDirty())
                {
                    Logger.Info($"Octrees of batch {batchOctrees.id} is dirty. Writing to temp save data prior saving to save slot.");
                    batchOctrees.WriteOctrees();
                }
            }
        }
	}
}
