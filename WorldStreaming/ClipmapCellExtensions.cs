using System;
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
        private static readonly FieldInfo chunkField = typeof(ClipmapCell).GetField("chunk", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo stateField = typeof(ClipmapCell).GetField("state", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly Type StateEnum = typeof(ClipmapCell).GetNestedType("State", BindingFlags.Public | BindingFlags.NonPublic);

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

            clipmapCell.level.streamer.streamingThread.Enqueue(new Task.Function(EndRebuildingMeshesTask), clipmapCell, meshBuilder);
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

        public static void EndRebuildingMeshesTask(object owner, object state)
        {
            var clipmapCell = (ClipmapCell)owner;
            var meshBuilder = (MeshBuilder)state;

            clipmapCell.level.streamer.buildLayersThread.Enqueue(new Task.Function(RebuildLayersTask), clipmapCell, meshBuilder);
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

            var oldClipmapChunk = chunkField.GetValue(clipmapCell) as ClipmapChunk;
            if (oldClipmapChunk && oldClipmapChunk.gameObject)
            {
                UnityEngine.Object.Destroy(oldClipmapChunk.gameObject);
            }

            chunkField.SetValue(clipmapCell, clipmapChunk);
        }

        public static bool IsVisible(this ClipmapCell clipmapCell)
        {
            var clipmapCellState = stateField.GetValue(clipmapCell) as Enum;
            var visibleState = Enum.Parse(StateEnum, "Visible") as Enum;

            Logger.Debug($"clipmapCellState {clipmapCellState}, visibleState {visibleState} => {clipmapCellState.Equals(visibleState)}");

            return clipmapCellState.Equals(visibleState);
        }

        public static bool IsLoadedState(this ClipmapCell clipmapCell)
        {
            var state = stateField.GetValue(clipmapCell) as Enum;

            var loadedState = Enum.Parse(StateEnum, "Loaded") as Enum;
            var visibleState = Enum.Parse(StateEnum, "Visible") as Enum;
            var hiddenByParentState = Enum.Parse(StateEnum, "HiddenByParent") as Enum;
            var hiddenByChildrenState = Enum.Parse(StateEnum, "HiddenByChildren") as Enum;

            throw new NotImplementedException("IsLoadedState() is not fully implemented");
        }
    }
}
