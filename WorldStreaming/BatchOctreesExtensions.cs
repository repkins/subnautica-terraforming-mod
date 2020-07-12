using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using WorldStreaming;

namespace Terraforming.WorldStreaming
{
    static class BatchOctreesExtensions
    {
        private static readonly FieldInfo octreesField = typeof(BatchOctrees).GetField("octrees", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo streamerField = typeof(BatchOctrees).GetField("streamer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo stateField = typeof(BatchOctrees).GetField("state", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly Type StateEnum = typeof(BatchOctrees).GetNestedType("State", BindingFlags.Public | BindingFlags.NonPublic);

        private static List<BatchOctrees> dirtyBatches = new List<BatchOctrees>();

        static public void SetOctree(this BatchOctrees batchOctrees, Int3 octreeId, VoxelandData.OctNode root)
        {
            batchOctrees.GetOctree(octreeId).Set(root);

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

        static public bool IsLoadedState(this BatchOctrees batchOctrees)
        {
            var state = stateField.GetValue(batchOctrees) as Enum;
            var loadedState = Enum.Parse(StateEnum, "Loaded") as Enum;

            Logger.Debug($"state {state}, loadedState {loadedState} => {state.Equals(loadedState)}");

            return state.Equals(loadedState);
        }
    }
}
