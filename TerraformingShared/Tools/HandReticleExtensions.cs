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
#if BelowZero
            return handReticle.textHandSubscript;
#else
            return handReticle.interactText2;
#endif
        }

        public static void SetHandSubscriptText(this HandReticle handReticle, string subscriptText)
        {
#if BelowZero
            handReticle.SetTextRaw(HandReticle.TextType.HandSubscript, subscriptText);
#else
            handReticle.SetInteractText(handReticle.interactText1, subscriptText);
#endif
        }
    }
}
