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
            results.RemoveAll(IsConstructionObstacle);
#if !BelowZero
            results.RemoveAll(IsImmuneToPropulsion);
#endif
        }

        static bool IsConstructionObstacle(GameObject go)
        {
            return go.GetComponent<ConstructionObstacle>() != null;
        }

        static bool IsImmuneToPropulsion(GameObject go)
        {
            return go.GetComponent<ImmuneToPropulsioncannon>() != null;
        }
    }
}
