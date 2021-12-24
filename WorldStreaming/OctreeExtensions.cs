using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.Collections;
using UWE;
using WorldStreaming;

namespace Terraforming.WorldStreaming
{
    static class OctreeExtensions
	{
		public static VoxelandData.OctNode ToVLOctree(this Octree octree)
		{
			return octree.ToVLOctNodeRecursive(0);
		}

		public static void Write(this Octree octree, BinaryWriter binaryWriter)
		{
			int octreeDataLength = 0;

			var octreeData = octree.data;
			if (octreeData != null)
			{
				octreeDataLength = octreeData.Length;
			}

			binaryWriter.Write(Convert.ToUInt16(octreeDataLength / 4));

			if (octreeData != null)
			{
				binaryWriter.Write(octreeData.ToArray());
			}
		}

		public static NativeArray<byte> GetData(this Octree octree)
		{
			return octree.data;
		}

		public static Int3 GetId(this Octree octree)
		{
			return octree.id;
		}

		private static VoxelandData.OctNode ToVLOctNodeRecursive(this Octree octree, int nid)
		{
			CompactOctree.Node node = octree.GetNode(nid);
			VoxelandData.OctNode octNode = node.ToVLNode();

			if (!octree.IsLeaf(nid))
			{
				octNode.childNodes = VoxelandData.OctNode.childNodesPool.Get();
				for (int i = 0; i < 8; i++)
				{
					octNode.childNodes[i] = octree.ToVLOctNodeRecursive(node.firstChildId + i);
				}
			}
			return octNode;
		}

		private static CompactOctree.Node GetNode(this Octree octree, int id)
		{
			return new CompactOctree.Node(octree.GetType(id), octree.GetDensity(id), Convert.ToUInt16(octree.GetFirstChildId(id)));
		}

		private static void SetNode(this Octree octree, int id, byte type, byte density, ushort firstChildId)
		{
			var octreeData = octree.data;

			int num = id * 4;
			octreeData[num] = type;
			octreeData[num + 1] = density;
			octreeData[num + 2] = Convert.ToByte(firstChildId & 255);
			octreeData[num + 3] = Convert.ToByte(firstChildId >> 8);
		}

		public static void Set(this Octree octree, VoxelandData.OctNode root, SplitNativeArrayPool<byte> allocator)
		{
			int num = root.CountNodes() * 4;

			octree.Clear(allocator);
			var octreeData = allocator.Get(num);

			octree.data = octreeData;

			ushort num2 = 1;
			octree.SetInternal(root, 0, ref num2);
		}

		private static void SetInternal(this Octree octree, VoxelandData.OctNode node, int nodeId, ref ushort nextFreeId)
		{
			if (node.IsLeaf())
			{
				octree.SetNode(nodeId, node.type, node.density, 0);
			}
			else
			{
				ushort num = nextFreeId;
				octree.SetNode(nodeId, node.type, node.density, num);
				nextFreeId += 8;
				for (int i = 0; i < 8; i++)
				{
					octree.SetInternal(node.childNodes[i], num + i, ref nextFreeId);
				}
			}
		}
	}
}
