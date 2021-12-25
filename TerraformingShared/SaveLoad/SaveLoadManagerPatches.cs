using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Terraforming.WorldStreaming;
using UnityEngine;

namespace Terraforming.SaveLoad.SaveLoadManagerPatches
{
    [HarmonyPatch(typeof(SaveLoadManager))]
    [HarmonyPatch("SaveToTemporaryStorageAsync")]
    [HarmonyPatch(new Type[] { typeof(IOut<SaveLoadManager.SaveResult>), typeof(Texture2D) })]
    static class SaveToTemporaryStorageAsyncPatch
    {
        static void Postfix(SaveLoadManager __instance, ref IEnumerator __result)
        {
            __result = PostfixAsync(__instance, __result);
        }

        static IEnumerator PostfixAsync(SaveLoadManager saveLoadManager, IEnumerator originalResult)
        {
            yield return originalResult;

            LargeWorldStreamer.main.frozen = true;
            saveLoadManager.isSaving = true;

            var octreesStreamer = LargeWorldStreamer.main.streamerV2.octreesStreamer;
            while (!octreesStreamer.IsIdle())
            {
                yield return null;
            }
            octreesStreamer.WriteBatchOctrees();

            LargeWorldStreamer.main.frozen = false;
            saveLoadManager.isSaving = false;

            yield break;
        }
    }
}
