using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using Terraforming;
using UnityEngine.Rendering;
using UnityEngine;
using System.Collections;
using Valve.VR;
using System.Linq;

namespace TerraformingShared.Tools.BuilderToolPatches
{
    [HarmonyPatch(typeof(BuilderTool))]
    [HarmonyPatch(nameof(BuilderTool.Update))]
    public static class UpdatePatch
    {
        public static bool storedEnabledDestroyingObstacles = Config.Instance.destroyLargerObstaclesOnConstruction;

        static readonly List<Renderer> obstacleRendererList = new List<Renderer>();

        static bool emptyPrevTarget = true;

        public static void Postfix()
        {
            bool destroyToggled = false;
            bool configChanged = false;
            if (!Builder.isPlacing && GameInput.GetButtonDown(GameInput.Button.AltTool) || GameInput.GetButtonUp(GameInput.Button.AltTool))
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
                        obstacleRendererList.ForEach(renderer => renderer.fadeAmount = 1f);
                        obstacleRendererList.Clear();

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
                obstacleRendererList.ForEach(renderer => renderer.fadeAmount = 1f);
                obstacleRendererList.Clear();

                emptyPrevTarget = true;
            }
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

                                foreach (var renderer in obstacleRendererList)
                                {
                                    renderer.fadeAmount = .1f;

                                    /*
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
                                    }*/
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