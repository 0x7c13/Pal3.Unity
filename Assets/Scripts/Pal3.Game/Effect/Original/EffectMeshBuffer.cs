// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect.Original
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Rendering;

    internal sealed class EffectMeshBuffer : IDisposable
    {
        private readonly Mesh _mesh;
        private readonly MeshRenderer _renderer;
        private readonly List<Vector3> _vertices = new();
        private readonly List<Vector2> _uv = new();
        private readonly List<Color32> _colors = new();
        private readonly List<int> _indices = new();

        public int QuadCount => _vertices.Count / 4;

        public EffectMeshBuffer(GameObject root, Material material)
        {
            _mesh = new Mesh { name = "Original effect mesh", indexFormat = IndexFormat.UInt32, hideFlags = HideFlags.DontSave };
            _mesh.MarkDynamic();
            root.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = root.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = material;
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.lightProbeUsage = LightProbeUsage.Off;
            _renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        public void Clear()
        {
            _vertices.Clear();
            _uv.Clear();
            _colors.Clear();
            _indices.Clear();
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 uv0, Vector2 uv1, Color32 color)
            => Quad(a, b, c, d, uv0, uv1, color, color, color, color);

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 uv0, Vector2 uv1,
            Color32 colorA, Color32 colorB, Color32 colorC, Color32 colorD)
        {
            int n = _vertices.Count;
            _vertices.Add(a);
            _vertices.Add(b);
            _vertices.Add(c);
            _vertices.Add(d);
            _uv.Add(uv0);
            _uv.Add(new Vector2(uv1.x, uv0.y));
            _uv.Add(uv1);
            _uv.Add(new Vector2(uv0.x, uv1.y));
            _colors.Add(colorA);
            _colors.Add(colorB);
            _colors.Add(colorC);
            _colors.Add(colorD);
            _indices.Add(n);
            _indices.Add(n + 1);
            _indices.Add(n + 2);
            _indices.Add(n);
            _indices.Add(n + 2);
            _indices.Add(n + 3);
        }

        public void Upload()
        {
            _renderer.enabled = _vertices.Count > 0;
            _mesh.Clear(false);
            _mesh.SetVertices(_vertices);
            _mesh.SetUVs(0, _uv);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_indices, 0, true);
        }

        public void Dispose() => OriginalEffectResources.Destroy(_mesh);
    }
}
