using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using WorldStreaming;

namespace Terraforming.WorldStreaming
{
    static class ClipmapStreamerExtensions
    {
		private static readonly FieldInfo levelsField = typeof(ClipmapStreamer).GetField("levels", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

		private static List<Int3.Bounds> rangesEdited = new List<Int3.Bounds>();

		public static void AddToRangesEdited(this ClipmapStreamer clipmapStreamer, Int3.Bounds blockRange)
		{
			rangesEdited.Add(blockRange);
			Logger.Info($"{blockRange} added. Currently has {rangesEdited.Count} ranges added.");
		}

		public static void FlushRangesEdited(this ClipmapStreamer clipmapStreamer, int minLod, int maxLod)
		{
			var rangesCount = rangesEdited.Count;

			clipmapStreamer.OnRangesEdited(rangesEdited, minLod, maxLod);
			rangesEdited.Clear();

			Logger.Info($"{rangesCount} ranges flushing");
		}

		private static void OnRangesEdited(this ClipmapStreamer clipmapStreamer, List<Int3.Bounds> blockRanges, int minLod, int maxLod)
		{
			var clipmapStreamerLevels = levelsField.GetValue(clipmapStreamer) as ClipmapLevel[];

			minLod = Mathf.Clamp(minLod, 0, clipmapStreamerLevels.Length - 1);
			maxLod = Mathf.Clamp(maxLod, 0, clipmapStreamerLevels.Length - 1);
			for (int i = minLod; i <= maxLod; i++)
			{
				clipmapStreamerLevels[i].OnBatchOctreesEdited(blockRanges);
			}
		}

	}
}
