using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Terraforming;
using UnityEngine;

namespace Terraforming.Tools.Building
{
    [HarmonyPatch(typeof(EntityCell))]
    static class EntityCellPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(EntityCell.AwakeAsync))]
        public static void AddDummyColliders(EntityCell __instance, ref IEnumerator __result)
        {
            __result = AddDummyCollidersAsync(__instance, __result);
        }

        private static IEnumerator AddDummyCollidersAsync(EntityCell entityCell, IEnumerator originalEnumerator)
        {
            yield return originalEnumerator;

            if (entityCell.liveRoot)
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var rootObjs = entityCell.liveRoot.GetComponentsInChildren<Renderer>()
                    .Select(renderer => renderer.GetComponentInParent<PrefabIdentifier>())
                    .Where(prefabIdentifier => prefabIdentifier)
                    .Select(prefabIdentifier => prefabIdentifier.gameObject)
                    .Where(rootObj => !rootObj.GetComponentInChildren<Collider>());

                foreach (var rootObj in rootObjs)
                {
                    var collidingBox = new GameObject("TerraformingHelper");
                    collidingBox.transform.parent = rootObj.transform;
                    collidingBox.transform.localPosition = Vector3.zero;
                    collidingBox.layer = LayerID.Useable;

                    var collider = collidingBox.AddComponent<BoxCollider>();
                    if (collider)
                    {
                        collider.isTrigger = true;
                        collider.size = Vector3.zero;
                    }

                    //var debugBox = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    //debugBox.transform.position = collidingBox.transform.position;
                    //debugBox.transform.localScale *= 0.25f;
                    //UnityEngine.Object.Destroy(debugBox.GetComponent<Collider>());

                    Logger.Info($"Adding collider object for {rootObj} at {rootObj.transform.position}");

                    yield return null;
                    //PrintStruct(rootObj.transform);
                }

                stopwatch.Stop();

                Logger.Debug($"AddDummyColliders for {entityCell} took {stopwatch.Elapsed.TotalMilliseconds} ms");
            }
        }

        private static void PrintStruct(Transform transform, int level = 0)
        {
            var prefix = "";
            for (var i = 0; i < level; i++)
            {
                prefix += "  ";
            }

            GameObject go = transform.gameObject;

            Logger.Debug($"{prefix}GameObject {go.transform.position} {go}:");

            Component[] components = go.GetComponents<Component>();
            foreach (var component in components)
            {
                Logger.Debug($"{prefix}  {component}");
            }

            foreach (Transform child in transform)
            {
                PrintStruct(child, level + 1);
            }
        }
    }
}
