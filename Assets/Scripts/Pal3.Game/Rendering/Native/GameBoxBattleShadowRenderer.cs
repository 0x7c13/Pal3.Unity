// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Native
{
    using System;
    using System.Collections.Generic;
    using Engine.Extensions;
    using UnityEngine;
    using UnityEngine.Rendering;

    /// <summary>Original combat texture quads, independent of Unity light shadows.</summary>
    public sealed class GameBoxBattleShadowRenderer : IDisposable
    {
        private readonly Mesh _mesh;
        private readonly Material _material;
        private readonly List<Vector3> _vertices = new();
        private readonly List<Vector2> _uv = new();
        private readonly List<int> _indices = new();

        public int Count => _vertices.Count / 4;

        public GameBoxBattleShadowRenderer(string variant, Texture texture)
        {
            bool pal3A = string.Equals(variant, "PAL3A", StringComparison.OrdinalIgnoreCase);
            if (!pal3A && !string.Equals(variant, "PAL3", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Unknown GameBox shadow variant.", nameof(variant));
            if (texture == null)
                throw new ArgumentNullException(nameof(texture));
            Shader shader = Shader.Find("Pal3/BattleShadow");
            if (shader == null)
                throw new InvalidOperationException("The battle shadow shader is missing.");
            _material = new Material(shader) { name = "Original battle shadow", hideFlags = HideFlags.DontSave, mainTexture = texture };
            // The two shipped shadow.gbf resources deliberately differ.
            _material.SetInt("_SrcBlend", (int)(pal3A ? BlendMode.OneMinusSrcAlpha : BlendMode.DstColor));
            _material.SetInt("_DstBlend", (int)(pal3A ? BlendMode.SrcAlpha : BlendMode.Zero));
            _mesh = new Mesh { name = "Original battle shadow quads", hideFlags = HideFlags.DontSave };
            _mesh.MarkDynamic();
        }

        public void Begin()
        {
            _vertices.Clear();
            _uv.Clear();
            _indices.Clear();
        }

        public void Add(GameBoxBattleShadow shadow)
        {
            int start = _vertices.Count;
            for (int index = 0; index < 4; index++)
                _vertices.Add(shadow.Corner(index));
            _uv.Add(new Vector2(0, 0));
            _uv.Add(new Vector2(1, 0));
            _uv.Add(new Vector2(1, 1));
            _uv.Add(new Vector2(0, 1));
            // GameBox splits the quad along 1--3. X mirroring reverses winding.
            _indices.Add(start);
            _indices.Add(start + 3);
            _indices.Add(start + 1);
            _indices.Add(start + 3);
            _indices.Add(start + 2);
            _indices.Add(start + 1);
        }

        public void End()
        {
            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetUVs(0, _uv);
            _mesh.SetTriangles(_indices, 0, true);
        }

        public void Render(Camera camera, int layer)
        {
            if (Count > 0)
                Graphics.DrawMesh(_mesh, Matrix4x4.identity, _material, layer, camera, 0, null,
                    ShadowCastingMode.Off, false, null, LightProbeUsage.Off);
        }

        public void Dispose()
        {
            _mesh.Destroy();
            _material.Destroy();
        }
    }
}
