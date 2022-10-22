using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using Terraforming;
using UnityEngine.Rendering;
using UnityEngine;
using System.Collections;
using System.Linq;

#if BelowZero
namespace TerraformingShared.Tools
{
    [HarmonyPatch(typeof(BuilderTool))]
    public static class BuilderToolPatches
    {
        static bool storedEnabledDestroyingObstacles = Config.Instance.destroyLargerObstaclesOnConstruction;

        static readonly List<Renderer> obstacleRendererList = new List<Renderer>();
        static GameObject prevTarget;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(BuilderTool.Update))]
        public static void UpdateObstacleSettings()
        {
            bool destroyToggled = false;
            bool configChanged = false;
            if (GameInput.GetButtonDown(GameInput.Button.AltTool) || GameInput.GetButtonUp(GameInput.Button.AltTool))
            {
                Config.Instance.destroyLargerObstaclesOnConstruction = !Config.Instance.destroyLargerObstaclesOnConstruction;
                destroyToggled = true;
            }

            if (!GameInput.GetButtonHeld(GameInput.Button.AltTool))
            {
                configChanged = storedEnabledDestroyingObstacles != Config.Instance.destroyLargerObstaclesOnConstruction;
                storedEnabledDestroyingObstacles = Config.Instance.destroyLargerObstaclesOnConstruction;
            }

            bool isConstructableTarget = false;

            Targeting.AddToIgnoreList(Player.main.gameObject);
            Targeting.GetTarget(30f, out var targetObject, out var num);
            if (targetObject != null)
            {
                var constructableBase = targetObject.GetComponentInParent<ConstructableBase>();
                var constructable = targetObject.GetComponentInParent<Constructable>();

                if (constructableBase != null && constructable != null && num <= constructable.placeMaxDistance)
                {
                    isConstructableTarget = true;

                    if (destroyToggled || configChanged || prevTarget != targetObject)
                    {
                        RestoreHighlightedObstacles();

                        if (Config.Instance.destroyLargerObstaclesOnConstruction)
                        {
                            HighlightDestroyableObstacles(constructableBase);
                        }
                    }

                    prevTarget = targetObject;
                }
            }

            if (!isConstructableTarget || Builder.isPlacing)
            {
                RestoreHighlightedObstacles();
                prevTarget = null;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(BuilderTool.OnHover), new Type[] { typeof(Constructable) })]
        public static void ShowObstaclesTooltip(Constructable constructable)
        {
            if (!constructable.constructed)
            {
                var enableText = "Enable Destroying Obstacles";
                var disableText = "Disable Destroying Obstacles";

                var storedDestroyingEnabled = storedEnabledDestroyingObstacles;

                var obstaclesUseText = string.Format("{0} (Hold {1})", storedDestroyingEnabled ? disableText : enableText, uGUI.FormatButton(GameInput.Button.AltTool));

                var handSubscriptText = HandReticle.main.textHandSubscript;
                handSubscriptText = handSubscriptText.Insert(handSubscriptText.IndexOf(Environment.NewLine), string.Format(", {0}", obstaclesUseText));

                HandReticle.main.SetTextRaw(HandReticle.TextType.HandSubscript, handSubscriptText);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(BuilderTool.OnHolster))]
        public static void OnHolster_Postfix()
        {
            RestoreHighlightedObstacles();
            prevTarget = null;
        }

        static void HighlightDestroyableObstacles(ConstructableBase constructableBase)
        {
            using (var obstacleListPool = Pool<ListPool<GameObject>>.Get())
            {
                var obstacleList = obstacleListPool.list;

                using (var orientedBoundsListPool = Pool<ListPool<OrientedBounds>>.Get())
                {
                    var orientedBoundsList = orientedBoundsListPool.list;
                    Builder.CacheBounds(constructableBase.gameObject.transform, constructableBase.gameObject, orientedBoundsList, false);
                    Builder.GetObstacles(constructableBase.transform.position, constructableBase.transform.rotation, orientedBoundsList, null, obstacleList);
                }

                if (obstacleList.Count > 0)
                {
                    using (var materialListPool = Pool<ListPool<Material>>.Get())
                    {
                        var materialList = materialListPool.list;

                        foreach (var obstacle in obstacleList)
                        {
                            if (!(obstacle.GetComponent<BaseCell>() != null) && Builder.CanDestroyObject(obstacle))
                            {
                                obstacle.GetComponentsInChildren(Builder.sRenderers);

                                obstacleRendererList.AddRange(Builder.sRenderers);
                                obstacleRendererList.ForEach(r => r.fadeAmount = Config.Instance.destroyableObstacleTransparency);
                            }
                        }
                    }
                }
            }
        }

        static void RestoreHighlightedObstacles()
        {
            obstacleRendererList.ForEach(renderer => renderer.fadeAmount = 1f);
            obstacleRendererList.Clear();
        }
    }
}
#endif