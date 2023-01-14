using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Terraforming;
using TerraformingShared.Tools.Building;
using UnityEngine;

namespace Terraforming.Tools.Building
{
    [HarmonyPatch(typeof(EntityCell))]
    static class EntityCellPatches
    {
        static Stopwatch stopwatch = new Stopwatch();

        [HarmonyPostfix]
        [HarmonyPatch(nameof(EntityCell.AwakeAsync))]
        public static IEnumerator AddColliders(IEnumerator originalEnumerator, EntityCell __instance)
        {
            yield return originalEnumerator;
            yield return AddCollidersAsync(__instance);
        }

        private static IEnumerator AddCollidersAsync(EntityCell entityCell)
        {
            if (entityCell.liveRoot)
            {
                stopwatch.Start();

                var collidersOfCell = BuilderExtensions.PassThroughObjectCollidersPerCell.GetOrAddNew(entityCell.GetTuple());
                collidersOfCell.Clear();

                var rootObjs = entityCell.liveRoot.GetComponentsInChildren<Renderer>()
                    .Select(renderer => renderer.GetComponentInParent<PrefabIdentifier>())
                    .Where(prefabIdentifier => prefabIdentifier)
                    .Select(prefabIdentifier => prefabIdentifier.gameObject)
                    .Where(rootObj => !rootObj.GetComponentInChildren<Collider>());

                foreach (var rootObj in rootObjs)
                {
                    var collidingBox = new GameObject(EntityCellExtensions.PassThroughColliderName);
                    collidingBox.transform.parent = rootObj.transform;
                    collidingBox.transform.localPosition = Vector3.zero;
                    collidingBox.layer = LayerID.Useable;

                    var collider = collidingBox.AddComponent<BoxCollider>();
                    if (collider)
                    {
                        collider.isTrigger = true;
                        collider.size = rootObj.GetComponentInChildren<Renderer>().bounds.size;
                        collider.enabled = false;
                    }

                    collidersOfCell.Add(collider);

                    Logger.Info($"Adding collider object for {rootObj} at {rootObj.transform.position}");

                    yield return null;
                }

                stopwatch.Stop();
                Logger.Debug($"Adding colliders of {entityCell} took {stopwatch.Elapsed.TotalMilliseconds} ms");

                stopwatch.Reset();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(EntityCell.SleepAsync))]
        public static IEnumerator ClearColliders(IEnumerator originalEnumerator, EntityCell __instance)
        {
            yield return originalEnumerator;
            yield return ClearCollidersAsync(__instance);
        }

        private static IEnumerator ClearCollidersAsync(EntityCell entityCell)
        {
            if (BuilderExtensions.PassThroughObjectCollidersPerCell.TryGetValue(entityCell.GetTuple(), out var collidersInCell))
            {
                if (collidersInCell.Count > 0)
                {
                    Logger.Debug($"Clearing {collidersInCell.Count} pass-through objects of {entityCell}.");

                    collidersInCell.Clear();
                }
            }

            yield break;
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
