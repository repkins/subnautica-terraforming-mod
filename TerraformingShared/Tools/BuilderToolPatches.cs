using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using Terraforming;
using UnityEngine.Rendering;
using UnityEngine;
using System.Collections;

namespace TerraformingShared.Tools.BuilderToolPatches
{
    [HarmonyPatch(typeof(BuilderTool))]
    [HarmonyPatch(nameof(BuilderTool.Update))]
    public static class UpdatePatch
    {
        public static bool storedEnabledDestroyingObstacles = Config.Instance.destroyLargerObstaclesOnConstruction;
        static Material destroyableObstacleMat = null;

        static bool emptyPrevTarget = true;

        public static void Postfix()
        {
            if (destroyableObstacleMat == null)
            {
                destroyableObstacleMat = new Material(Builder.builderObstacleMaterial);
                destroyableObstacleMat.SetColor(ShaderPropertyID._Tint, Color.red);
            }

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

                    if (destroyToggled || configChanged || emptyPrevTarget)
                    {
                        Builder.obstaclesBuffer.Clear();
                        if (Config.Instance.destroyLargerObstaclesOnConstruction)
                        {
                            HighlightDestroyableObstacles(constructableBase);
                        }
                    }

                    emptyPrevTarget = false;
                }
            }

            if (!isConstructableTarget)
            {
                Builder.obstaclesBuffer.Clear();
                emptyPrevTarget = true;
            }
        }

        static void HighlightDestroyableObstacles(ConstructableBase constructableBase)
        {
            using (var obstacleListPool = Pool<ListPool<GameObject>>.Get())
            {
                var obstacleList = obstacleListPool.list;

                using (var constructableBoundsListPool = Pool<ListPool<ConstructableBounds>>.Get())
                {
                    var constructableBoundsList = constructableBoundsListPool.list;
                    constructableBase.GetComponentsInChildren(true, constructableBoundsList);

                    using (var overlappedObjectsListPool = Pool<ListPool<GameObject>>.Get())
                    {
                        var overlappedObjectsList = overlappedObjectsListPool.list;

                        foreach (var constructableBounds in constructableBoundsList)
                        {
                            var orientedBounds = OrientedBounds.ToWorldBounds(constructableBounds.transform, constructableBounds.bounds);
                            overlappedObjectsList.Clear();
                            Builder.GetOverlappedObjects(orientedBounds.position, orientedBounds.rotation, orientedBounds.extents, overlappedObjectsList);

                            obstacleList.AddRange(overlappedObjectsList);
                        }
                    }
                }

                if (obstacleList.Count > 0)
                {
                    using (var materialListPool = Pool<ListPool<Material>>.Get())
                    {
                        var materialList = materialListPool.list;

                        foreach (var obstacle in obstacleList)
                        {
                            if (!(obstacle.GetComponent<BaseCell>() != null))
                            {
                                Builder.sRenderers.Clear();
                                obstacle.GetComponentsInChildren(Builder.sRenderers);
                                foreach (var renderer in Builder.sRenderers)
                                {
                                    if (renderer.enabled && renderer.shadowCastingMode != ShadowCastingMode.ShadowsOnly && !(renderer is ParticleSystemRenderer))
                                    {
                                        renderer.GetSharedMaterials(materialList);
                                        var shaderPassNum = 0;
                                        foreach (var material in materialList)
                                        {
                                            shaderPassNum++;
                                            if (!(material == null) && !Builder.shadersToExclude.Contains(material.shader) && material.renderQueue < 2450
                                                && (!material.HasProperty(ShaderPropertyID._EnableCutOff)
                                                || material.GetFloat(ShaderPropertyID._EnableCutOff) <= 0f)
                                                && !material.IsKeywordEnabled("FX_BUILDING"))
                                            {
                                                Builder.obstaclesBuffer.DrawRenderer(renderer, destroyableObstacleMat, shaderPassNum);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
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