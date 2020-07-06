using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Terraforming.WorldLegacyStreaming
{
    class OctreesEditData
    {
        public Int3.Bounds localBlockBounds { get; private set; }
        public VoxelandData.OctNode.BlendArgs blendArgs { get; private set; }
        public byte type { get; private set; }
        public LargeWorldStreamer.DistanceField df { get; private set; }

        public Matrix4x4 localToWorldMatrix { get; private set; }

        public OctreesEditData(Int3.Bounds localBlockBounds, bool isAdd, byte type, LargeWorldStreamer.DistanceField df)
        {
            this.localBlockBounds = localBlockBounds;
            this.blendArgs = new VoxelandData.OctNode.BlendArgs(isAdd ? VoxelandData.OctNode.BlendOp.Union : VoxelandData.OctNode.BlendOp.Subtraction, false, isAdd ? type : (byte)0); ;
            this.type = type;
            this.df = df;

            localToWorldMatrix = LargeWorldStreamer.main.land.transform.localToWorldMatrix;
        }
    }
}
