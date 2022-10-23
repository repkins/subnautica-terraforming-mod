using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TerraformingShared.Tools
{
    internal static class BuilderExtensions
    {
        public static void ClearConstructionObstacles(List<GameObject> results)
        {
            results.RemoveAll(IsObstacleOf<ConstructionObstacle>);
#if !BelowZero
            results.RemoveAll(IsObstacleOf<ImmuneToPropulsioncannon>);
#endif
        }

        static bool IsObstacleOf<T>(GameObject go)
        {
            return go.GetComponent<T>() != null;
        }

        public static void GetObstacles(Vector3 position, Quaternion rotation, List<OrientedBounds> localBounds, List<GameObject> results)
        {
#if !BelowZero
            Builder.GetObstacles(position, rotation, localBounds, results);
#else
            Builder.GetObstacles(position, rotation, localBounds, null, results);
#endif
        }
    }
}
