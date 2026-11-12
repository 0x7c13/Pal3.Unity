// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Pol
{
    using System;
    using System.Collections.Generic;

    /// <summary>Remove unused vertices without welding UV seams or front/back normal groups.</summary>
    public sealed class PolMeshIndexMap
    {
        public int[] SourceIndices { get; }
        public int[] Triangles { get; }

        public PolMeshIndexMap(int vertexCount, int[] triangles)
        {
            if (vertexCount < 0) throw new ArgumentOutOfRangeException(nameof(vertexCount));
            if (triangles == null || triangles.Length % 3 != 0)
                throw new ArgumentException("POL indices must contain complete triangles", nameof(triangles));
            var map = new Dictionary<int, int>();
            var source = new List<int>();
            Triangles = new int[triangles.Length];
            for (int i = 0; i < triangles.Length; i++)
            {
                int index = triangles[i];
                if ((uint)index >= (uint)vertexCount)
                    throw new ArgumentOutOfRangeException(nameof(triangles), "POL vertex index is out of range");
                if (!map.TryGetValue(index, out int compactIndex))
                {
                    compactIndex = source.Count;
                    map.Add(index, compactIndex);
                    source.Add(index);
                }
                Triangles[i] = compactIndex;
            }
            SourceIndices = source.ToArray();
        }
    }
}
