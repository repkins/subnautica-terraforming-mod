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
        public static void Patch()
        {
            var harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), "subnautica.repkins.terraforming");
            Logger.Info("Successfully patched");

            Config.Load();
            Logger.Info("Config successfully loaded");
        }
    }
}
