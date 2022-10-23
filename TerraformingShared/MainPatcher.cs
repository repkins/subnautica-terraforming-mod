using HarmonyLib;
using HarmonyLib.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Terraforming.Resources;
using static HarmonyLib.Tools.Logger;

namespace Terraforming
{
    public static class MainPatcher
    {
        public static void Patch()
        {
            Config.Load();
            Logger.Info("Config successfully loaded");

            ChannelFilter = LogChannel.Error | LogChannel.Warn;
            HarmonyFileLog.Enabled = true;

            var harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Identifiers.HarmonyId);
            Logger.Info("Successfully patched");
        }
    }
}
