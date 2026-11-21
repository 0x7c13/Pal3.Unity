// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Renderer
{
    using System.Collections.Generic;
    using Core.DataReader.Pol;
    using UnityEngine;

    /// <summary>Geometry sharing one material and one local spatial cell within a POL instance.</summary>
    internal sealed class PolMeshBatch
    {
        internal const float CellSize = 32f;
        internal const int MaximumVertices = 65535;
        internal readonly List<Vector3> Positions = new();
        internal readonly List<Vector3> Normals = new();
        internal readonly List<Color32> Colors = new();
        internal readonly List<Vector2> MainUvs = new();
        internal readonly List<Vector2> LightmapUvs = new();
        internal readonly List<int> Triangles = new();
#if UNITY_EDITOR
        internal readonly List<Dev.Presenters.PolMeshBatchInfoPresenter.SourceGroup> Sources = new();
#endif

        internal void Add(PolMeshIndexMap map, Vector3[] positions, Vector3[] normals,
            Vector2[] mainUvs, Vector2[] lightmapUvs, Color32[] colors = null, bool geometricNormals = false)
        {
            int offset = Positions.Count;
            Vector3[] generatedNormals = geometricNormals ? CaptureGroupNormals(map, positions) : null;
            int local = 0;
            foreach (int source in map.SourceIndices)
            {
                Positions.Add(positions[source]);
                if (generatedNormals != null)
                    Normals.Add(generatedNormals[local]);
                else if (normals != null)
                    Normals.Add(normals[source]);
                local++;
                if (colors != null)
                    Colors.Add(colors[source]);
                MainUvs.Add(mainUvs[source]);
                LightmapUvs.Add(lightmapUvs[source]);
            }
            // GameBox and Unity have opposite winding. Preserve triangle order within each group.
            for (int i = map.Triangles.Length - 1; i >= 0; i--)
                Triangles.Add(offset + map.Triangles[i]);
        }

        // Reconstruct only within the selected material group. Original exports
        // can smooth a level floor into an adjoining painted vertical backdrop.
        internal static Vector3[] CaptureGroupNormals(PolMeshIndexMap map, Vector3[] positions)
        {
            var normals = new Vector3[map.SourceIndices.Length];
            for (int index = 0; index < map.Triangles.Length; index += 3)
            {
                int a = map.Triangles[index + 2];
                int b = map.Triangles[index + 1];
                int c = map.Triangles[index];
                Vector3 face = Vector3.Cross(positions[map.SourceIndices[b]] - positions[map.SourceIndices[a]],
                    positions[map.SourceIndices[c]] - positions[map.SourceIndices[a]]);
                normals[a] += face;
                normals[b] += face;
                normals[c] += face;
            }
            for (int index = 0; index < normals.Length; index++)
                normals[index] = normals[index].normalized;
            return normals;
        }

        internal static bool TryGetCell(PolMeshIndexMap map, Vector3[] positions, out Vector3Int cell)
        {
            var bounds = new Bounds(positions[map.SourceIndices[0]], Vector3.zero);
            foreach (int source in map.SourceIndices)
                bounds.Encapsulate(positions[source]);
            cell = Vector3Int.FloorToInt(bounds.center / CellSize);
            // Large sections stay separate so batching cannot turn a whole scene into one bound.
            return bounds.size.x <= CellSize && bounds.size.y <= CellSize && bounds.size.z <= CellSize;
        }
    }
}
