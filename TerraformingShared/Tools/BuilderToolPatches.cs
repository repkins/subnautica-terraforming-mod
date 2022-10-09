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
        public static bool storedEnabledDestroyingObstacles = Config.Instance.destroyLargerObstaclesOnConstruction;

        public static void Postfix()
        {
            if (GameInput.GetButtonDown(GameInput.Button.AltTool) || GameInput.GetButtonUp(GameInput.Button.AltTool))
            {
                Config.Instance.destroyLargerObstaclesOnConstruction = !Config.Instance.destroyLargerObstaclesOnConstruction;
            }

            if (!GameInput.GetButtonHeld(GameInput.Button.AltTool))
            {
                storedEnabledDestroyingObstacles = Config.Instance.destroyLargerObstaclesOnConstruction;
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
                var enableText = "Enable Destroying Obstacles";
                var disableText = "Disable Destroying Obstacles";

                var storedDestroyingEnabled = UpdatePatch.storedEnabledDestroyingObstacles;

                var obstaclesUseText = string.Format("{0} (Hold {1})", storedDestroyingEnabled ? disableText : enableText, uGUI.FormatButton(GameInput.Button.AltTool));

                var handSubscriptText = HandReticle.main.textHandSubscript;
                handSubscriptText = handSubscriptText.Insert(handSubscriptText.IndexOf(Environment.NewLine), string.Format(", {0}", obstaclesUseText));

                HandReticle.main.SetTextRaw(HandReticle.TextType.HandSubscript, handSubscriptText);
            }
        }
    }
}