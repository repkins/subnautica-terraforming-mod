using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraforming.Resources;
using UnityEngine.Events;

#if !BelowZero
using Terraforming.Shims;
#endif

namespace Terraforming.Configuration
{
    public static class uGuiOptionsPanelPatches
    {
        static readonly string modsLabel = $"Mods";

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

                    __instance.AddToggleOption(modsTabIndex, Texts.RebuildingMessages, Config.Instance.rebuildMessages,
                        new UnityAction<bool>(value => Config.Instance.rebuildMessages = value),
                        $"Shows terrain rebuilding message while terrain rebuilding is in progress. Enabled by default."
                    );

                    __instance.AddToggleOption(modsTabIndex, Texts.HabitatModulesBurying, Config.Instance.habitantModulesPartialBurying,
                        new UnityAction<bool>(value => Config.Instance.habitantModulesPartialBurying = value),
                        $"Allows habitat burying into terrain and adjusts overlapping terrain around them. Enabled by default."
                    );

                    __instance.AddSliderOption(modsTabIndex, Texts.TerrainVsModuleSpace, Config.Instance.spaceBetweenTerrainHabitantModule,
                        0.0f, 10.0f,
                        DefaultConfig.spaceBetweenTerrainHabitantModule,
                        0.5f,
                        new UnityAction<float>(value => Config.Instance.spaceBetweenTerrainHabitantModule = value),
                        SliderLabelMode.Float,
                        "0.0",
                        $"Allows to adjust space between terrain surface and base compartment. High value means more space, low value means less space. Defaults to 1.0."
                    );

                    __instance.AddToggleOption(modsTabIndex, Texts.RepulsionTerrainImpact, Config.Instance.terrainImpactWithRepulsionCannon,
                        new UnityAction<bool>(value => Config.Instance.terrainImpactWithRepulsionCannon = value),
                        $"Causes the repulsion cannon to remove small portion of terrain after \"shooting\" pulse to spot. Enabled by default."
                    );

                    __instance.AddSliderOption(modsTabIndex, Texts.DestroyableObstacleTransparency, Config.Instance.destroyableObstacleTransparency,
                        0.0f, 1.0f,
                        DefaultConfig.destroyableObstacleTransparency,
                        0.01f,
                        new UnityAction<float>(value => Config.Instance.destroyableObstacleTransparency = value),
                        SliderLabelMode.Percent,
                        "000",
                        $"Allows to adjust transparency amount of destroyable construction obstacles. Transparency serves as warning to be destroyed if destroying obstacles enabled. Defaults to 10."
                    );

                    __instance.AddToggleOption(modsTabIndex, Texts.DestroyObstacles, Config.Instance.destroyLargerObstaclesOnConstruction,
                        new UnityAction<bool>(value => Config.Instance.destroyLargerObstaclesOnConstruction = value),
                        $"Disables restrictions of overlapping larger objects with placable habitat module. Destroys them when construction of module finishes. Disabled by default."
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
