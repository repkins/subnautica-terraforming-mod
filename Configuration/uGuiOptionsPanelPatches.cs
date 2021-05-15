using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Events;

namespace Terraforming.Configuration
{
    public static class uGuiOptionsPanelPatches
    {
        static string modsLabel = $"Mods";

        static int? nullableModsTabIndex;

        [HarmonyPatch(typeof(uGUI_TabbedControlsPanel))]
        [HarmonyPatch(nameof(uGUI_TabbedControlsPanel.AddTab))]
        public static class AddTabPatch
        {
            [HarmonyPostfix]
            public static void Postfix(uGUI_TabbedControlsPanel __instance, int __result, string label)
            {
                if (label == modsLabel)
                {
                    nullableModsTabIndex = __result;
                }
            }
        }

        [HarmonyPatch(typeof(uGUI_OptionsPanel))]
        [HarmonyPatch(nameof(uGUI_OptionsPanel.AddTabs))]
        public static class AddTabsPatch
        {
            [HarmonyPostfix]
            public static void Postfix(uGUI_OptionsPanel __instance)
            {
                if (nullableModsTabIndex.HasValue)
                {
                    var modsTabIndex = nullableModsTabIndex.Value;

                    __instance.AddHeading(modsTabIndex, $"Terraforming");

                    __instance.AddToggleOption(modsTabIndex, $"Rebuilding messages", Config.Instance.rebuildMessages,
                        new UnityAction<bool>(value => Config.Instance.rebuildMessages = value),
                        $"Shows terrain rebuilding message while terrain rebuilding is in progress"
                    );

                    __instance.AddToggleOption(modsTabIndex, $"Habitant modules burying", Config.Instance.habitantModulesPartialBurying,
                        new UnityAction<bool>(value => Config.Instance.habitantModulesPartialBurying = value),
                        $"Allows habitat burying into terrain and adjusts overlapping terrain around them"
                    );

                    __instance.AddSliderOption(modsTabIndex, $"Space between terrain and module", Config.Instance.spaceBetweenTerrainHabitantModule,
                        0.0f, 10.0f,
                        DefaultConfig.spaceBetweenTerrainHabitantModule,
                        0.5f,
                        new UnityAction<float>(value => Config.Instance.spaceBetweenTerrainHabitantModule = value),
                        SliderLabelMode.Float,
                        "0"
                    );

                    __instance.AddToggleOption(modsTabIndex, $"Propulsion cannon terrain impact", Config.Instance.terrainImpactWithPropulsionCannon,
                        new UnityAction<bool>(value => Config.Instance.terrainImpactWithPropulsionCannon = value),
                        $"Causes the repulsion cannon to remove small portion of terrain after \"shooting\" pulse to spot"
                    );

                    __instance.AddToggleOption(modsTabIndex, $"Destroy obstacles on construction", Config.Instance.destroyLargerObstaclesOnConstruction,
                        new UnityAction<bool>(value => Config.Instance.destroyLargerObstaclesOnConstruction = value),
                        $"Disables restrictions of overlapping larger objects with placable habitat module. Destroys them when construction of module finishes."
                    );
                }
            }
        }

        [HarmonyPatch(typeof(uGUI_TabbedControlsPanel))]
        [HarmonyPatch(nameof(uGUI_TabbedControlsPanel.SetVisibleTab))]
        public static class SetVisibleTabPatch
        {
            [HarmonyPrefix]
            public static void Prefix(uGUI_TabbedControlsPanel __instance, int tabIndex)
            {
                if (nullableModsTabIndex.HasValue)
                {
                    var modsTabIndex = nullableModsTabIndex.Value;

                    // If navigating from Mods tab...
                    if (__instance.currentTab == modsTabIndex && tabIndex != modsTabIndex)
                    {
                        Config.Save();
                    }
                }
            }
        }

        [HarmonyPatch(typeof(uGUI_OptionsPanel))]
        [HarmonyPatch(nameof(uGUI_OptionsPanel.OnDisable))]
        public static class OnDisablePatch
        {
            [HarmonyPostfix]
            public static void Postfix(uGUI_OptionsPanel __instance)
            {
                if (nullableModsTabIndex.HasValue)
                {
                    var modsTabIndex = nullableModsTabIndex.Value;

                    // If at Mods tab...
                    if (__instance.currentTab == modsTabIndex)
                    {
                        Config.Save();
                    }
                }
            }
        }
    }
}
