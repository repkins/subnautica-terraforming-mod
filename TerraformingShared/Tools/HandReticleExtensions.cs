using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terraforming.Tools
{
    internal static class HandReticleExtensions
    {
        public static string GetHandSubscript(this HandReticle handReticle)
        {
            return handReticle.textHandSubscript;
        }

        public static void SetHandSubscriptText(this HandReticle handReticle, string subscriptText)
        {
            handReticle.SetTextRaw(HandReticle.TextType.HandSubscript, subscriptText);
        }
    }
}
