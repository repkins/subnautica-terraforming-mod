using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Terraforming.Shims
{
    internal static class TabbedControlsPanelShims
    {
        public static Toggle AddToggleOption(this uGUI_TabbedControlsPanel controlsPanel, int tabIndex, string label, bool value, UnityAction<bool> callback = null, string tooltip = null)
        {
            return controlsPanel.AddToggleOption(tabIndex, label, value, callback);
        }

        public static void AddSliderOption(this uGUI_TabbedControlsPanel controlsPanel, int tabIndex, string label, float value, float minValue, float maxValue, float defaultValue, float step, UnityAction<float> callback, SliderLabelMode labelMode, string floatFormat, string tooltip = null)
        {
            controlsPanel.AddSliderOption(tabIndex, label, value, minValue, maxValue, defaultValue, callback);
        }
    }

    internal enum SliderLabelMode
    {
        Default,
        Percent,
        Int,
        Float,
        Delegate
    }
}
