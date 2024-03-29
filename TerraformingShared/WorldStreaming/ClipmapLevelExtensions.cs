﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Terraforming.Messaging;
using UnityEngine;
using UWE;
using WorldStreaming;

namespace Terraforming.WorldStreaming
{
    static class ClipmapLevelExtensions
    {
        private const string rebuildingTerrainMsg = "Rebuilding terrain area...";

        private static Dictionary<int, int> remainingCellCount = new Dictionary<int, int>();
        private static Dictionary<int, Dictionary<Int3, ClipmapChunk>> nullableClipmapChunks = new Dictionary<int, Dictionary<Int3, ClipmapChunk>>();

        private static ErrorMessage._Message rebuildingMessage = null;
        public static bool isMeshesRebuilding = false;

        public static void OnBatchOctreesEdited(this ClipmapLevel clipmapLevel, List<Int3.Bounds> blockRanges)
        {
            var processingCells = clipmapLevel.GetProcessingCells(blockRanges);

            if (!remainingCellCount.ContainsKey(clipmapLevel.id))
            {
                remainingCellCount[clipmapLevel.id] = 0;
            }

            if (remainingCellCount.All((levelCountPair) => levelCountPair.Value <= 0))
            {
                isMeshesRebuilding = true;
                if (Config.Instance.rebuildMessages)
                {
                    rebuildingMessage = ErrorMessageExtensions.AddReturnMessage(rebuildingTerrainMsg);
                }
            }

            remainingCellCount[clipmapLevel.id] += processingCells.Count;
            if (!nullableClipmapChunks.ContainsKey(clipmapLevel.id))
            {
                nullableClipmapChunks[clipmapLevel.id] = new Dictionary<Int3, ClipmapChunk>();
            }

            Logger.Debug($"{clipmapLevel}: Setting remainingCellCount to {remainingCellCount[clipmapLevel.id]}");

            foreach (var cell in processingCells)
            {
                cell.OnBatchOctreesEdited();
            }
        }

        public static void OnEndBuildLayers(this ClipmapLevel clipmapLevel, ClipmapCell clipmapCell, ClipmapChunk nullableClipmapChunk)
        {
            nullableClipmapChunks[clipmapLevel.id][clipmapCell.id] = nullableClipmapChunk;

            remainingCellCount[clipmapLevel.id]--;
            Logger.Debug($"Decrementing remainingCellCount to {remainingCellCount[clipmapLevel.id]}");

            if (remainingCellCount[clipmapLevel.id] <= 0)
            {
                clipmapLevel.SwapChunks(nullableClipmapChunks[clipmapLevel.id]);
                nullableClipmapChunks[clipmapLevel.id].Clear();

                if (remainingCellCount.All((levelCountPair) => levelCountPair.Value <= 0))
                {
                    isMeshesRebuilding = false;
                    Logger.Debug($"{clipmapLevel}: Mesh building finished with 'remainingCellCount' of {remainingCellCount[clipmapLevel.id]}");

                    if (rebuildingMessage != null)
                    {
                        ErrorMessageExtensions.SetMessageTimeEnd(rebuildingMessage, PDA.time);
                        ErrorMessageExtensions.pendingMessageToRemove = rebuildingMessage;
                        rebuildingMessage = null;
                    }
                }
            }
            else if (rebuildingMessage != null)
            {
                var oldTimeEnd = ErrorMessageExtensions.GetMessageTimeEnd(rebuildingMessage);
                var timeFadeOut = ErrorMessageExtensions.GetTimeFadeOut();
                if (oldTimeEnd - timeFadeOut < PDA.time)
                {
                    rebuildingMessage = ErrorMessageExtensions.AddReturnMessage(rebuildingTerrainMsg);
                }
            }
        }

        private static void SwapChunks(this ClipmapLevel clipmapLevel, Dictionary<Int3, ClipmapChunk> clipmapChunks)
        {
            Logger.Info($"{clipmapLevel}: Swapping chunks");

            foreach (var cellChunkPair in clipmapChunks)
            {
                var cell = clipmapLevel.GetCell(cellChunkPair.Key);
                var clipmapChunk = cellChunkPair.Value;

                if (cell != null && cell.IsLoaded())
                {
                    cell.SwapChunk(clipmapChunk);
                }
                else
                {
                    Logger.Warning($"Clipmap Cell {cellChunkPair.Key} is not loaded or null, skipping swapping chunks and destroying new one");
                    UnityEngine.Object.Destroy(clipmapChunk);
                }
            }
        }

        private static List<ClipmapCell> GetProcessingCells(this ClipmapLevel clipmapLevel, List<Int3.Bounds> blockRanges)
        {
            var processingCells = new List<ClipmapCell>();

            foreach (var blockRange in blockRanges)
            {
                var cellBounds = clipmapLevel.GetCellRange(blockRange);

                foreach (Int3 cellId in cellBounds)
                {
                    ClipmapCell cell = clipmapLevel.GetCell(cellId);
                    if (cell != null)
                    {
                        if (!processingCells.Contains(cell))
                        {
                            processingCells.Add(cell);
                        }
                    }
                }
            }

            return processingCells;
        }
    }
}
