using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Terraforming.Messaging;
using UnityEngine;
using UWE;
using WorldStreaming;

namespace Terraforming.WorldStreaming
{
    static class WorldStreamerExtensions
    {
		private static FieldInfo streamingThreadField = typeof(WorldStreamer).GetField("streamingThread", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

		private const string editingTerrainMsg = "Editing terrain data...";

		private static List<OctreesEditData> octreesEditDatas = new List<OctreesEditData>();

		private static object editingMessage = null;
		public static bool isOctreesEditing = false;

		public static void AddToWorldEdit(this WorldStreamer worldStreamer, Int3.Bounds localBlockBounds, LargeWorldStreamer.DistanceField df, bool isAdd = false, byte type = 1)
		{
			localBlockBounds = localBlockBounds.Expanded(1);
			var octreesEditData = new OctreesEditData(localBlockBounds, isAdd, type, df);

			octreesEditDatas.Add(octreesEditData);
		}

		public static void FlushWorldEdit(this WorldStreamer worldStreamer)
		{
			var streamingThread = streamingThreadField.GetValue(worldStreamer) as IThread;

			streamingThread.Enqueue(new Task.Function(PerformWorldEditTask), worldStreamer, octreesEditDatas);

			octreesEditDatas = new List<OctreesEditData>();
		}

		public static void PerformWorldEditTask(object owner, object state)
		{
			var worldStreamer = (WorldStreamer)owner;
			var octreesEditDatas = (List<OctreesEditData>)state;

			worldStreamer.PerformWorldEdit(octreesEditDatas);
		}

		public static void PerformWorldEdit(this WorldStreamer worldStreamer, List<OctreesEditData> octreesEditDatas)
		{
			while (worldStreamer.IsIdle())
			{ }

			if (octreesEditDatas.Any(editData => !worldStreamer.octreesStreamer.IsRangeLoadedState(editData.localBlockBounds)))
			{
				Logger.Warning($"Area of {octreesEditDatas.Count} ranges to edit is not entirely loaded, skipping.");
				ErrorMessage.AddMessage("Area is too large to edit.");
			}
			else
			{
				isOctreesEditing = true;
				if (Config.Instance.rebuildMessages)
				{
					editingMessage = ErrorMessageExtensions.AddReturnMessage(editingTerrainMsg);
				}

				foreach (var octreesEditData in octreesEditDatas)
				{
					worldStreamer.octreesStreamer.PerformOctreesEdit(octreesEditData);
					worldStreamer.clipmapStreamer.AddToRangesEdited(octreesEditData.localBlockBounds);

					if (editingMessage != null)
					{
						var oldTimeEnd = ErrorMessageExtensions.GetMessageTimeEnd(editingMessage);
						var timeFadeOut = ErrorMessageExtensions.GetTimeFadeOut();
						if (oldTimeEnd - timeFadeOut < Time.time)
						{
							editingMessage = ErrorMessageExtensions.AddReturnMessage(editingTerrainMsg);
						}
					}
				}

				isOctreesEditing = false;
				if (editingMessage != null)
				{
					ErrorMessageExtensions.SetMessageTimeEnd(editingMessage, Time.time);
					ErrorMessageExtensions.pendingMessageToRemove = editingMessage;
					editingMessage = null;
				}

				worldStreamer.clipmapStreamer.FlushRangesEdited(worldStreamer.octreesStreamer.minLod, worldStreamer.octreesStreamer.maxLod);
			}
		}
	}
}
