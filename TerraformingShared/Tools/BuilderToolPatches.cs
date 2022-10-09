using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using Terraforming;

namespace TerraformingShared.Tools.BuilderToolPatches
{
    [HarmonyPatch(typeof(BuilderTool))]
    [HarmonyPatch(nameof(BuilderTool.Update))]
    public static class UpdatePatch
    {
        public static void Postfix()
        {
            if (GameInput.GetButtonDown(GameInput.Button.AltTool) || GameInput.GetButtonUp(GameInput.Button.AltTool))
            {
                Config.Instance.destroyLargerObstaclesOnConstruction = !Config.Instance.destroyLargerObstaclesOnConstruction;
            }
        }
    }

    [HarmonyPatch(typeof(BuilderTool))]
    [HarmonyPatch(nameof(BuilderTool.UpdateCustomUseText))]
    public static class UpdateCustomUseTextPatch
    {
        public static void Postfix(BuilderTool __instance)
        {
            if (Builder.isPlacing)
            {
                var obstaclesUseText = string.Format("Toggle Destroy Obstacles (Hold {0})", uGUI.FormatButton(GameInput.Button.AltTool));

                __instance.customUseText = __instance.customUseText.Replace("\n", string.Format(", {0}\n", obstaclesUseText));
            }
        }
    }

    [HarmonyPatch(typeof(BuilderTool))]
    [HarmonyPatch(nameof(BuilderTool.OnHover))]
    [HarmonyPatch(new Type[] { typeof(Constructable) })]
    public static class OnHoverPatch
    {
        public static void Postfix(BuilderTool __instance, Constructable constructable)
        {
            if (!constructable.constructed)
            {
                var obstaclesUseText = string.Format("Toggle Destroy Obstacles (Hold {0})", uGUI.FormatButton(GameInput.Button.AltTool));
                var handSubscriptText = HandReticle.main.textHandSubscript;
                handSubscriptText = handSubscriptText.Insert(handSubscriptText.IndexOf(Environment.NewLine), string.Format(", {0}", obstaclesUseText));

                HandReticle.main.SetTextRaw(HandReticle.TextType.HandSubscript, handSubscriptText);
            }
        }
    }
}