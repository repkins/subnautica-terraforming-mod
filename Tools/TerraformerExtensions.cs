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
        public static Stack<GameObject> GetStrokePool(this Terraformer terraformer)
        {
            return terraformer.strokePool;
        }

        public static GameObject GetProbe(this Terraformer terraformer)
        {
            return terraformer.probe;
        }
    }
}
