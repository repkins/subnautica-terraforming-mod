using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Terraforming.Tools
{
    static class TerraformerExtensions
    {
        private static readonly FieldInfo strokePoolField = typeof(Terraformer).GetField("strokePool", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo probeField = typeof(Terraformer).GetField("probe", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        public static Stack<GameObject> GetStrokePool(this Terraformer terraformer)
        {
            return strokePoolField.GetValue(terraformer) as Stack<GameObject>;
        }

        public static GameObject GetProbe(this Terraformer terraformer)
        {
            return probeField.GetValue(terraformer) as GameObject;
        }
    }
}
