// See LICENSE in the project root.

namespace Editor
{
    using System;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    /// <summary>
    /// Keeps normal Save Project operations from persisting temporary MSAA and shadow budgets.
    /// SaveAssetIfDirty bypasses OnWillSaveAssets and is outside this safeguard.
    /// </summary>
    [InitializeOnLoad]
    internal sealed class RuntimePipelineAssetSaveGuard : AssetModificationProcessor
    {
        private const string SnapshotKey = "Pal3.RuntimePipelineAssetSaveGuard.v1";

        [Serializable]
        private sealed class Snapshot
        {
            public string Guid;
            public int Msaa;
            public int ShadowAtlas;
        }

        static RuntimePipelineAssetSaveGuard()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode) RestoreAndClear();
            };
        }

        private static Snapshot ReadSnapshot()
        {
            string json = SessionState.GetString(SnapshotKey, "");
            return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<Snapshot>(json);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                SessionState.EraseString(SnapshotKey);
                var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                string guid = pipeline != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(pipeline)) : "";
                if (string.IsNullOrEmpty(guid)) return;
                SessionState.SetString(SnapshotKey, JsonUtility.ToJson(new Snapshot
                {
                    Guid = guid,
                    Msaa = pipeline.msaaSampleCount,
                    ShadowAtlas = pipeline.additionalLightsShadowmapResolution,
                }));
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Restore();
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                RestoreAndClear();
            }
        }

        private static void Restore()
        {
            Snapshot snapshot = ReadSnapshot();
            if (snapshot == null) return;
            string path = AssetDatabase.GUIDToAssetPath(snapshot.Guid);
            if (string.IsNullOrEmpty(path)) return;
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (pipeline == null) return;
            pipeline.msaaSampleCount = snapshot.Msaa;
            pipeline.additionalLightsShadowmapResolution = snapshot.ShadowAtlas;
        }

        private static void RestoreAndClear()
        {
            Restore();
            SessionState.EraseString(SnapshotKey);
        }

        private static string[] OnWillSaveAssets(string[] paths)
        {
            Snapshot snapshot = ReadSnapshot();
            if (!EditorApplication.isPlayingOrWillChangePlaymode && snapshot == null) return paths;

            string originalPath = snapshot != null ? AssetDatabase.GUIDToAssetPath(snapshot.Guid) : "";
            // When this script is first imported during Play, there may be no pre-Play snapshot.
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            string currentPath = pipeline != null ? AssetDatabase.GetAssetPath(pipeline) : "";
            return paths.Where(path => path != originalPath && path != currentPath).ToArray();
        }
    }
}
