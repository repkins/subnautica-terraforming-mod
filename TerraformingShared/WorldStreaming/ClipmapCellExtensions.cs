using System;
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

#if BelowZero
            CoroutineHost.StartCoroutine(clipmapCell.RebuildLayersAsync(meshBuilder));
#else
            clipmapCell.RebuildLayers(meshBuilder, out var clipmapChunk);
            clipmapCell.level.OnEndBuildLayers(clipmapCell, clipmapChunk);
#endif
        }

#if BelowZero
        public static IEnumerator RebuildLayersAsync(this ClipmapCell clipmapCell, MeshBuilder meshBuilder)
        {
            Logger.Debug($"{clipmapCell}: Begin");

            ClipmapChunk nullableClipmapChunk = null;
            if (clipmapCell.streamer != null && clipmapCell.streamer.host != null)
            {
                var host = clipmapCell.level.streamer.host;
                nullableClipmapChunk = meshBuilder.DoFinalizePart(host.chunkRoot, host.terrainPoolManager);
                clipmapCell.streamer.meshBuilderPool.Return(meshBuilder);

                yield return clipmapCell.ActivateChunkAndCollider(nullableClipmapChunk);
            }
            clipmapCell.level.OnEndBuildLayers(clipmapCell, nullableClipmapChunk);

            Logger.Debug($"{clipmapCell}: End");

            yield break;
        }
#else
        public static void RebuildLayers(this ClipmapCell clipmapCell, MeshBuilder meshBuilder, out ClipmapChunk clipmapChunk)
        {
            Logger.Debug($"{clipmapCell}: Begin");

            var host = clipmapCell.level.streamer.host;
            clipmapChunk = meshBuilder.DoFinalizePart(host.chunkRoot, host.chunkPrefab, host.chunkLayerPrefab);

            clipmapCell.level.streamer.meshBuilderPool.Return(meshBuilder);

            Logger.Debug($"{clipmapCell}: End");
        }
#endif

        public static void SwapChunk(this ClipmapCell clipmapCell, ClipmapChunk nullableClipmapChunk)
        {
            if (clipmapCell.IsVisible())
            {
                if (nullableClipmapChunk)
                {
                    Logger.Debug($"{clipmapCell}: Showing new chunk");
                    nullableClipmapChunk.Show();
                }
            }

            var oldClipmapChunk = clipmapCell.chunk;
            if (oldClipmapChunk)
            {
#if BelowZero
                if (!clipmapCell.streamer.host.terrainPoolManager.meshPoolingEnabled)
                {
                    MeshBuilder.DestroyMeshes(oldClipmapChunk);
                }
                clipmapCell.ReturnChunkToPool(oldClipmapChunk);
#else
                MeshBuilder.DestroyMeshes(oldClipmapChunk);
                if (oldClipmapChunk.gameObject)
                {
                    UnityEngine.Object.Destroy(oldClipmapChunk.gameObject);
                }
#endif
            }

            clipmapCell.chunk = nullableClipmapChunk;
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
