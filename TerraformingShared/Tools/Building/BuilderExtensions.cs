using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TerraformingShared.Tools.Building
{
    internal static class BuilderExtensions
    {
        public static readonly Dictionary<(int, Int3, Int3), List<Collider>> PassThroughObjectCollidersPerCell = new Dictionary<(int, Int3, Int3), List<Collider>>();

        public static void ClearConstructionObstacles(List<GameObject> results)
        {
            results.RemoveAll(IsRogueContructionObstacle);
        }

        public static bool IsRogueContructionObstacle(GameObject go)
        {
            if (IsObstacleOf<ConstructionObstacle>(go))
            {
                return true;
            }
#if !BelowZero
            if (IsObstacleOf<ImmuneToPropulsioncannon>(go))
            {
                return true;
            }
#endif
            return false;
        }

        static bool IsObstacleOf<T>(GameObject go)
        {
            return go.GetComponent<T>() != null;
        }

        public static void GetObstacles(Vector3 position, Quaternion rotation, List<OrientedBounds> localBounds, List<GameObject> results)
        {
            Builder.GetObstacles(position, rotation, localBounds, null, results);
        }
    }
}
