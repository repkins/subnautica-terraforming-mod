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
			global::Utils.PlayOneShotPS("VFX/xTerraform", position, Quaternion.Euler(new Vector3(270f, 0f, 0f)), null, 1f);
			if (LargeWorldStreamer.main != null)
			{
				LargeWorldStreamer.main.PerformSphereEdit(position, Mathf.Abs(range), range < 0f, 1);

				var streamerV2 = LargeWorldStreamer.main.streamerV2;
				streamerV2.FlushWorldEdit();
			}
		}
	}
}
