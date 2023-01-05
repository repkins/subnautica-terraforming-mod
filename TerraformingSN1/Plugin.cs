using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terraforming
{
    [BepInPlugin("subnautica.repkins.terraforming", "Terraforming", "1.5.0.0")]
    public class Plugin: BaseUnityPlugin
    {
        public void Awake()
        {
            MainPatcher.Patch();
        }
    }
}
