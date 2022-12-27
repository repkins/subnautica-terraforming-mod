using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Terraforming;
using Terraforming.WorldStreaming;

#if !BelowZero
namespace TerraformingShared.SaveLoad
{
    [HarmonyPatch(typeof(UserStoragePC))]
    static class UserStoragePCPatches
    {
        [HarmonyPatch(nameof(UserStoragePC.UpgradeSaveData))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> RemoveDeletingOctrees(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase originalMethod)
        {
            var codeMatcherCursor = new CodeMatcher(instructions);
            
            Action<CodeMatcher, ILGenerator>[] patchers =
            {
                AssignEmptyArrayToOctreesLocal
            };

            try
            {
                foreach (var patcher in patchers)
                {
                    patcher.Invoke(codeMatcherCursor, generator);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());

                codeMatcherCursor.ReportFailure(originalMethod, Logger.Error);

                return instructions;
            }

            return codeMatcherCursor.InstructionEnumeration();
        }

        private static void AssignEmptyArrayToOctreesLocal(CodeMatcher codeCursor, ILGenerator generator)
        {
            List<Label> labels;

            codeCursor.Start();
            codeCursor.MatchForward(useEnd: false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldstr, "compiled-batch-*.optoctrees"),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Directory), nameof(Directory.GetFiles), new[] { typeof(string), typeof(string), typeof(SearchOption) }))
            );
            codeCursor.ThrowIfInvalid("Could not find call to Directory.GetFiles with octrees search pattern");

            labels = codeCursor.Instruction.ExtractLabels();

            codeCursor.RemoveInstructions(4)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UserStoragePCPatches), nameof(GetEmptyArray))).WithLabels(labels));
        }

        private static string[] GetEmptyArray()
        {
            return new string[] { };
        }

        [HarmonyPatch(nameof(UserStoragePC.UpgradeSaveDataAsync))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> SkipOctrees(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase originalMethod)
        {
            var codeMatcherCursor = new CodeMatcher(instructions);

            Action<CodeMatcher, ILGenerator>[] patchers =
            {
                RemoveOctreesPattern
            };

            try
            {
                foreach (var patcher in patchers)
                {
                    patcher.Invoke(codeMatcherCursor, generator);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());

                codeMatcherCursor.ReportFailure(originalMethod, Logger.Error);

                return instructions;
            }

            return codeMatcherCursor.InstructionEnumeration();
        }

        private static void RemoveOctreesPattern(CodeMatcher codeCursor, ILGenerator generator)
        {
            List<Label> labels;

            codeCursor.Start();
            codeCursor.MatchForward(useEnd: false,
                new CodeMatch(OpCodes.Brtrue),
                new CodeMatch(OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Ldstr, "compiled-batch-*.optoctrees"),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Directory), nameof(Directory.EnumerateFiles), new[] { typeof(string), typeof(string), typeof(SearchOption) })),
                new CodeMatch(OpCodes.Call)
            )
            .ThrowIfInvalid("Could not find condition with call to Directory.EnumerateFiles uses octrees search pattern");

            labels = codeCursor.Instruction.ExtractLabels();

            codeCursor.RemoveInstructions(6);

            codeCursor.Instruction.WithLabels(labels);
        }

        [HarmonyPatch(nameof(UserStoragePC.UpgradeSaveDataAsync))]
        [HarmonyPostfix]
        static void EnsureCompiledOctreesBackFromBackup(UserStoragePC __instance, string backupPath, ref UserStorageUtils.UpgradeOperation __result)
        {
            __result = EnsureCompiledOctreesRestoredFromBackupAsync(__instance, backupPath, __result);
        }

        private static UserStorageUtils.UpgradeOperation EnsureCompiledOctreesRestoredFromBackupAsync(UserStoragePC userStoragePC, string backupPath, UserStorageUtils.UpgradeOperation originalOperation)
        {
            if (!string.IsNullOrEmpty(backupPath) && Directory.Exists(backupPath))
            {
                string compiledOctreesDirName = BatchOctreesStreamerExtensions.CompiledOctreesDirName;

                var saveContainersToRestore = new List<string>();
                foreach (string saveSlotDir in Directory.GetDirectories(userStoragePC.savePath))
                {
                    string saveFileName = Path.GetFileName(saveSlotDir);
                    string saveCompiledOctreesPath = Path.Combine(saveSlotDir, compiledOctreesDirName);
                    string backupSaveCompiledOctreesPath = Path.Combine(backupPath, saveFileName, compiledOctreesDirName);

                    // Restore save slot if exists in backup and is empty in target save location
                    if (Directory.Exists(backupSaveCompiledOctreesPath) && !Directory.EnumerateFiles(saveCompiledOctreesPath).Any())
                    {
                        saveContainersToRestore.Add(saveSlotDir);
                    }
                }

                var restoreOperation = new UserStorageUtils.UpgradeOperation
                {
                    itemsTotal = originalOperation.itemsTotal,
                    itemsPrecessed = originalOperation.itemsPrecessed,
                    result = originalOperation.result,
                    errorMessage = originalOperation.errorMessage
                };
                UserStoragePC.ioThread.Enqueue(RestoreCompiledOctreesFromBackup, userStoragePC, new UserStoragePC.UpgradeWrapper(restoreOperation, userStoragePC.savePath, backupPath, saveContainersToRestore));
                return restoreOperation;
            }
            else
            {
                Logger.Warning(string.Format("Backup dir is not created."));
            }
            return originalOperation;
        }

        private static void RestoreCompiledOctreesFromBackup(object owner, object state)
        {
            var upgradeWrapper = (UserStoragePC.UpgradeWrapper)state;
            var upgradeOperation = (UserStorageUtils.UpgradeOperation)upgradeWrapper.operation;

            string backupPath = upgradeWrapper.backupPath;
            string compiledOctreesDirName = BatchOctreesStreamerExtensions.CompiledOctreesDirName;
            foreach (var saveSlotPath in upgradeWrapper.containersToUpgrade)
            {
                string saveFileName = Path.GetFileName(saveSlotPath);
                string backupSaveCompiledOctreesPath = Path.Combine(backupPath, saveFileName, compiledOctreesDirName);

                if (Directory.Exists(backupSaveCompiledOctreesPath))
                {
                    try
                    {
                        string newSaveCompiledOctreesPath = Path.Combine(saveSlotPath, compiledOctreesDirName);
                        UWE.Utils.CopyDirectory(backupSaveCompiledOctreesPath, newSaveCompiledOctreesPath);

                        Logger.Info(string.Format("Compiled octrees restored from backup for save {0}.", saveFileName));
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning(string.Format("Failed to restore octrees at save {0}: {1}", saveFileName, ex.Message));
                    }
                }
                else
                {
                    Logger.Warning(string.Format("Backup save compiled-octrees dir does not exist at: {0}.", backupSaveCompiledOctreesPath));
                }
            }

            upgradeOperation.done = true;
        }
    }
}
#endif
