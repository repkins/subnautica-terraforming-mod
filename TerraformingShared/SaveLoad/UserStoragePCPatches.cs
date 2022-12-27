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
                var saveContainersToRestore = new List<string>();
                foreach (string saveSlotDir in Directory.GetDirectories(userStoragePC.savePath))
                {
                    string saveFileName = Path.GetFileName(saveSlotDir);
                    string saveCompiledOctreesPath = Path.Combine(saveSlotDir, BatchOctreesStreamerExtensions.CompiledOctreesDirName);
                    string saveCompiledOctreesIndicationFilePath = Path.Combine(saveCompiledOctreesPath, SaveLoadExtensions.IndicationFileName);
                    string backupSaveCompiledOctreesPath = Path.Combine(backupPath, saveFileName, BatchOctreesStreamerExtensions.CompiledOctreesDirName);

                    // Restore save slot if exists in backup and there is no indication file in target octrees location
                    if (Directory.Exists(backupSaveCompiledOctreesPath) && !File.Exists(saveCompiledOctreesIndicationFilePath))
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
                        string saveCompiledOctreesPath = Path.Combine(saveSlotPath, compiledOctreesDirName);

                        Directory.CreateDirectory(saveCompiledOctreesPath);
                        foreach (var backupSaveCompiledOctreesFilePath in Directory.EnumerateFiles(backupSaveCompiledOctreesPath, "compiled-batch-*.optoctrees"))
                        {
                            var oldFileName = Path.GetFileName(backupSaveCompiledOctreesFilePath);
                            var newFileName = oldFileName.Replace("compiled-batch", BatchOctreesStreamerExtensions.CompiledOctreesFileNamePrefix);

                            var saveCompiledOctreesFilePath = Path.Combine(saveCompiledOctreesPath, newFileName);

                            File.Copy(backupSaveCompiledOctreesFilePath, saveCompiledOctreesFilePath, true);
                        }

                        string saveCompiledOctreesIndicationFilePath = Path.Combine(saveCompiledOctreesPath, SaveLoadExtensions.IndicationFileName);
                        File.WriteAllBytes(saveCompiledOctreesIndicationFilePath, new byte[0]);

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
