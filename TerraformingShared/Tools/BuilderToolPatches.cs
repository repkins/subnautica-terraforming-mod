using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using Terraforming;
using UnityEngine.Rendering;
using UnityEngine;
using System.Collections;
using System.Linq;
using Terraforming.Tools;

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

                        HighlightDestroyableObstacles(constructableBase);
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

                var handSubscriptText = HandReticle.main.GetHandSubscript();
                handSubscriptText = handSubscriptText.Insert(handSubscriptText.IndexOf(Environment.NewLine), string.Format(", {0}", obstaclesUseText));

                HandReticle.main.SetHandSubscriptText(handSubscriptText);
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
            using (var constructableBoundsListPool = Pool<ListPool<ConstructableBounds>>.Get())
            using (var obstacleListPool = Pool<ListPool<GameObject>>.Get())
            using (var overlappedObjectsPool = Pool<ListPool<GameObject>>.Get())
            {
                var constructableBoundsList = constructableBoundsListPool.list;
                var obstacleList = obstacleListPool.list;
                var overlappedObjects = overlappedObjectsPool.list;

                constructableBase.GetComponentsInChildren(true, constructableBoundsList);

                var orientedBoundsList = constructableBoundsList.Select(constructableBounds => OrientedBounds.ToWorldBounds(constructableBounds.transform, constructableBounds.bounds));
                foreach (var orientedBounds in orientedBoundsList)
                {
                    overlappedObjects.Clear();
                    Builder.GetOverlappedObjects(orientedBounds.position, orientedBounds.rotation, orientedBounds.extents, overlappedObjects);

                    obstacleList.AddRange(overlappedObjects);
                }

                Terraforming.Logger.Debug($"obstacleList.Count = {obstacleList.Count}");

                if (obstacleList.Count > 0)
                {
                    using (var rendererListPool = Pool<ListPool<Renderer>>.Get())
                    {
                        var rendererList = rendererListPool.list;

                        foreach (var obstacle in obstacleList)
                        {
                            if ((Config.Instance.destroyLargerObstaclesOnConstruction || !BuilderExtensions.IsContructionObstacle(obstacle))
                                && obstacle.GetComponent<BaseCell>() == null && Builder.CanDestroyObject(obstacle))
                            {
                                obstacle.GetComponentsInChildren(rendererList);

                                obstacleRendererList.AddRange(rendererList);
                            }
                        }
                    }
                }

                obstacleRendererList.ForEach(r => r.fadeAmount = Config.Instance.destroyableObstacleTransparency);
            }
        }

        static void RestoreHighlightedObstacles()
        {
            obstacleRendererList.Where(renderer => renderer != null).ForEach(renderer => renderer.fadeAmount = 1f);
            obstacleRendererList.Clear();
        }
    }
}