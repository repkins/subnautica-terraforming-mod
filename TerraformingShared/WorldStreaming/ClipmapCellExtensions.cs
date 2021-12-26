﻿using System;
using System.Collections;
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

            var clipmapStreamer = clipmapCell.streamer;
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

            CoroutineHost.StartCoroutine(clipmapCell.RebuildLayersAsync(meshBuilder));
            clipmapCell.level.OnEndBuildLayers(clipmapCell, clipmapChunk);
        }

        public static IEnumerator RebuildLayersAsync(this ClipmapCell clipmapCell, MeshBuilder meshBuilder)
        public static void RebuildLayers(this ClipmapCell clipmapCell, MeshBuilder meshBuilder, out ClipmapChunk clipmapChunk)
        {
            Logger.Debug($"{clipmapCell}: Begin");

            ClipmapChunk nullableClipmapChunk = null;
            if (clipmapCell.streamer != null && clipmapCell.streamer.host != null)
            {
                var host = clipmapCell.level.streamer.host;
                nullableClipmapChunk = meshBuilder.DoFinalizePart(host.chunkRoot, host.terrainPoolManager);
                clipmapCell.streamer.meshBuilderPool.Return(meshBuilder);
            clipmapChunk = meshBuilder.DoFinalizePart(host.chunkRoot, host.chunkPrefab, host.chunkLayerPrefab);

                yield return clipmapCell.ActivateChunkAndCollider(nullableClipmapChunk);
            }
            clipmapCell.level.OnEndBuildLayers(clipmapCell, nullableClipmapChunk);
            clipmapCell.level.streamer.meshBuilderPool.Return(meshBuilder);

            Logger.Debug($"{clipmapCell}: End");

            yield break;
        }

        public static void SwapChunk(this ClipmapCell clipmapCell, ClipmapChunk nullableClipmapChunk)
        public static void SwapChunk(this ClipmapCell clipmapCell, ClipmapChunk clipmapChunk)
        {
            if (clipmapCell.IsVisible())
            {
                if (nullableClipmapChunk)
                {
                    Logger.Debug($"{clipmapCell}: Showing new chunk");
                    nullableClipmapChunk.Show();
                }
                clipmapChunk.Show();
            }

            var oldClipmapChunk = clipmapCell.chunk;
            if (oldClipmapChunk)
            {
                if (!clipmapCell.streamer.host.terrainPoolManager.meshPoolingEnabled)
                {
                    MeshBuilder.DestroyMeshes(oldClipmapChunk);

                if (oldClipmapChunk.gameObject)
                {
                    UnityEngine.Object.Destroy(oldClipmapChunk.gameObject);
                }
                clipmapCell.ReturnChunkToPool(oldClipmapChunk);
            }

            clipmapCell.chunk = nullableClipmapChunk;
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
