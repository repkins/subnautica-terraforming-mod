using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terraforming
{
    [BepInPlugin("subnautica-zero.repkins.terraforming", "TerraformingBZ", "1.4.4.1")]
    public class Plugin: BaseUnityPlugin
    {
        public void Awake()
        {
            MainPatcher.Patch();
        }
    }
}
