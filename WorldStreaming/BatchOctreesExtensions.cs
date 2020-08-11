﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UWE;
using WorldStreaming;

namespace Terraforming.WorldStreaming
{
    static class BatchOctreesExtensions
    {
        private static readonly FieldInfo octreesField = typeof(BatchOctrees).GetField("octrees", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo streamerField = typeof(BatchOctrees).GetField("streamer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo allocatorField = typeof(BatchOctrees).GetField("allocator", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        private static List<BatchOctrees> dirtyBatches = new List<BatchOctrees>();

        static public void SetOctree(this BatchOctrees batchOctrees, Int3 octreeId, VoxelandData.OctNode root)
        {
            var allocator = allocatorField.GetValue(batchOctrees) as LinearArrayHeap<byte>;

            var octree = batchOctrees.GetOctree(octreeId);
            octree.Set(root, allocator);

            if (!dirtyBatches.Contains(batchOctrees))
            {
                dirtyBatches.Add(batchOctrees);
            }
        }

        static public void WriteOctrees(this BatchOctrees batchOctrees)
        {
            var streamer = streamerField.GetValue(batchOctrees) as BatchOctreesStreamer;

            var tmpPath = streamer.GetTmpPath(batchOctrees.id);

            using (var binaryWriter = new BinaryWriter(File.OpenWrite(tmpPath)))
            {
                var version = 4;
                binaryWriter.WriteInt32(version);
                foreach (Octree octree in octreesField.GetValue(batchOctrees) as Array3<Octree>)
                {
                    octree.Write(binaryWriter);
                }
            }

            dirtyBatches.Remove(batchOctrees);
        }

        static public bool GetIsDirty(this BatchOctrees batchOctrees)
        {
            return dirtyBatches.Contains(batchOctrees);
        }
    }
}
