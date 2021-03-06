﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UWE;
using WorldStreaming;

namespace Terraforming.WorldStreaming
{
    static class ClipmapCellExtensions
    {
        public static void OnBatchOctreesEdited(this ClipmapCell clipmapCell)
        {
            Logger.Debug($"Enqueing RebuildMeshTask of {clipmapCell}");
            clipmapCell.level.streamer.meshingThreads.Enqueue(new Task.Function(RebuildMeshTask), clipmapCell, null);
            Logger.Debug($"End enqueing RebuildMeshTask of {clipmapCell}");
        }

        private static void RebuildMeshTask(object owner, object state)
        {
            var clipmapCell = (ClipmapCell)owner;
            clipmapCell.RebuildMesh(out var meshBuilder);

            clipmapCell.level.streamer.buildLayersThread.Enqueue(new Task.Function(RebuildLayersTask), clipmapCell, meshBuilder);
        }

        public static void RebuildMesh(this ClipmapCell clipmapCell, out MeshBuilder meshBuilder)
        {
            Logger.Debug($"{clipmapCell}: Begin");

            var clipmapStreamer = clipmapCell.level.streamer;
            var octreesStreamer = clipmapStreamer.host.GetOctreesStreamer(clipmapCell.level.id);

            meshBuilder = clipmapStreamer.meshBuilderPool.Get();
            meshBuilder.Reset(clipmapCell.level.id, clipmapCell.id, clipmapCell.level.cellSize, clipmapCell.level.settings, clipmapStreamer.host.blockTypes);
            meshBuilder.DoThreadablePart(octreesStreamer, clipmapStreamer.settings.collision);

            Logger.Debug($"{clipmapCell}: End");
        }

        private static void RebuildLayersTask(object owner, object state)
        {
            var clipmapCell = (ClipmapCell)owner;
            var meshBuilder = (MeshBuilder)state;
            clipmapCell.RebuildLayers(meshBuilder, out var clipmapChunk);

            clipmapCell.level.OnEndBuildLayers(clipmapCell, clipmapChunk);
        }

        public static void RebuildLayers(this ClipmapCell clipmapCell, MeshBuilder meshBuilder, out ClipmapChunk clipmapChunk)
        {
            Logger.Debug($"{clipmapCell}: Begin");

            var host = clipmapCell.level.streamer.host;
            clipmapChunk = meshBuilder.DoFinalizePart(host.chunkRoot, host.chunkPrefab, host.chunkLayerPrefab);

            clipmapCell.level.streamer.meshBuilderPool.Return(meshBuilder);

            Logger.Debug($"{clipmapCell}: End");
        }

        public static void SwapChunk(this ClipmapCell clipmapCell, ClipmapChunk clipmapChunk)
        {
            if (clipmapCell.IsVisible())
            {
                Logger.Debug($"{clipmapCell}: Showing new chunk");
                clipmapChunk.Show();
            }

            var oldClipmapChunk = clipmapCell.chunk;
            if (oldClipmapChunk)
            {
                MeshBuilder.DestroyMeshes(oldClipmapChunk);

                if (oldClipmapChunk.gameObject)
                {
                    UnityEngine.Object.Destroy(oldClipmapChunk.gameObject);
                }
            }

            clipmapCell.chunk = clipmapChunk;
        }

        public static bool IsVisible(this ClipmapCell clipmapCell)
        {
            var clipmapCellState = clipmapCell.state;
            var visibleState = ClipmapCell.State.Visible;

            Logger.Debug($"clipmapCellState {clipmapCellState}, visibleState {visibleState} => {clipmapCellState.Equals(visibleState)}");

            return clipmapCellState.Equals(visibleState);
        }
    }
}
