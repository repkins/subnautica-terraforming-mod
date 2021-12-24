using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Terraforming.Messaging.ErrorMessagePatches
{
    [HarmonyPatch(typeof(ErrorMessage))]
    [HarmonyPatch("ReleaseEntry")]
    static class ReleaseEntryPatch
    {
        static void Prefix()
        {
            var pendingMessageToRemove = ErrorMessageExtensions.pendingMessageToRemove;
            if (pendingMessageToRemove != null)
            {
                ErrorMessageExtensions.RemoveOffsetY(ErrorMessageExtensions.GetMessageEntry(pendingMessageToRemove).preferredHeight);

                ErrorMessageExtensions.pendingMessageToRemove = null;
            }
        }
    }
}
