using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraforming.WorldStreaming;
using UnityEngine;
using UWE;

namespace Terraforming
{
    static class Utils
    {
		public static void Terraform(Vector3 position, float range)
		{
			if (LargeWorldStreamer.main != null)
			{
				LargeWorldStreamer.main.PerformSphereEdit(position, Mathf.Abs(range), range < 0f, 0);

				var streamerV2 = LargeWorldStreamer.main.streamerV2;
				streamerV2.clipmapStreamer.FlushRangesEdited(streamerV2.octreesStreamer.minLod, streamerV2.octreesStreamer.maxLod);
            }
		}
	}
}
