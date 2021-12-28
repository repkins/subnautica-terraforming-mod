using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Terraforming
{
    public static class MainPatcher
    {
#if BelowZero
        private const string HarmonyInstanceId = "subnautica.repkins.terraforming.bz";
#else
        private const string HarmonyInstanceId = "subnautica.repkins.terraforming";
#endif

        public static void Patch()
        {
            Config.Load();
            Logger.Info("Config successfully loaded");

            var harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), HarmonyInstanceId);
            Logger.Info("Successfully patched");
        }
    }
}
