using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraforming.WorldStreaming;
using UnityEngine;

namespace Terraforming
{
    static class Utils
    {
		public static void Terraform(Vector3 position, float range)
		{
#if !BelowZero
			global::Utils.PlayOneShotPS("VFX/xTerraform", position, Quaternion.Euler(new Vector3(270f, 0f, 0f)), null, 1f);
#endif
			if (LargeWorldStreamer.main != null)
			{
#if BelowZero
				LargeWorldStreamer.main.PerformSphereEdit(position, Mathf.Abs(range), range < 0f, 0);
#else
				LargeWorldStreamer.main.PerformSphereEdit(position, Mathf.Abs(range), range < 0f, 1);
#endif

				var streamerV2 = LargeWorldStreamer.main.streamerV2;
				streamerV2.clipmapStreamer.FlushRangesEdited(streamerV2.octreesStreamer.minLod, streamerV2.octreesStreamer.maxLod);
            }
		}
	}
}
