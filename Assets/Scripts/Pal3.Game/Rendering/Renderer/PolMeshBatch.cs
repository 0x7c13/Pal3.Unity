// See LICENSE in the project root.

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
        internal readonly List<Vector2> MainUvs = new();
        internal readonly List<Vector2> LightmapUvs = new();
        internal readonly List<int> Triangles = new();
        #if UNITY_EDITOR
        internal readonly List<Dev.Presenters.PolMeshBatchInfoPresenter.SourceGroup> Sources = new();
        #endif

        internal void Add(PolMeshIndexMap map, Vector3[] positions, Vector3[] normals,
            Vector2[] mainUvs, Vector2[] lightmapUvs)
        {
            int offset = Positions.Count;
            foreach (int source in map.SourceIndices)
            {
                Positions.Add(positions[source]);
                if (normals != null) Normals.Add(normals[source]);
                MainUvs.Add(mainUvs[source]);
                LightmapUvs.Add(lightmapUvs[source]);
            }
            // GameBox and Unity have opposite winding. Preserve triangle order within each group.
            for (int i = map.Triangles.Length - 1; i >= 0; i--)
                Triangles.Add(offset + map.Triangles[i]);
        }

        internal static bool TryGetCell(PolMeshIndexMap map, Vector3[] positions, out Vector3Int cell)
        {
            var bounds = new Bounds(positions[map.SourceIndices[0]], Vector3.zero);
            foreach (int source in map.SourceIndices) bounds.Encapsulate(positions[source]);
            cell = Vector3Int.FloorToInt(bounds.center / CellSize);
            // Large sections stay separate so batching cannot turn a whole scene into one bound.
            return bounds.size.x <= CellSize && bounds.size.y <= CellSize && bounds.size.z <= CellSize;
        }
    }
}
