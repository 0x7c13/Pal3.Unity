// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Renderer
{
    using System;
    using Core.DataReader.Pol;
    using Engine.Extensions;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.SceneManagement;

    // gbBladeMgr/BladeRecord: 40 samples, duplicate the newest frame in
    // slots 0/1, and replace slot 0 between sample insertions.
    public sealed class WeaponBlade : IDisposable
    {
        private const int MaximumFrames = 40;
        private readonly Vector3[][] _frames = new Vector3[MaximumFrames][];
        private readonly float[] _times = new float[MaximumFrames];
        private readonly bool[] _fresh = new bool[MaximumFrames];
        private readonly TagNode[] _tags;
        private readonly Vector3[] _vertices;
        private readonly Color32[] _colors;
        private readonly int[] _triangles;
        private int _count;
        private bool _created;
        private float _time, _lifetime = 0.1f;
        private readonly Mesh _mesh;
        private readonly Mesh[] _flushMeshes = new Mesh[2];
        private readonly MeshRenderer[] _flushRenderers = new MeshRenderer[2];

        public WeaponBlade(TagNode[] tags)
        {
            _tags = tags ?? throw new ArgumentNullException(nameof(tags));
            _vertices = new Vector3[MaximumFrames * tags.Length];
            _colors = new Color32[_vertices.Length];
            _triangles = new int[(MaximumFrames - 1) * Math.Max(0, tags.Length - 1) * 6];
            int cursor = 0;
            for (int frame = 0; frame < MaximumFrames - 1; frame++)
                for (int index = 0; index < tags.Length - 1; index++)
                {
                    int first = frame * tags.Length + index, next = first + tags.Length;
                    _triangles[cursor++] = first;
                    _triangles[cursor++] = next + 1;
                    _triangles[cursor++] = next;
                    _triangles[cursor++] = first;
                    _triangles[cursor++] = first + 1;
                    _triangles[cursor++] = next + 1;
                }
            _mesh = new Mesh { name = "Original weapon blade", hideFlags = HideFlags.DontSave };
            _mesh.MarkDynamic();
        }

        public int TriangleCount => Math.Max(0, _count - 1) * Math.Max(0, _tags.Length - 1) * 2;

        public bool TryGetLastPosition(out Vector3 position)
        {
            position = _count > 0 && _frames[0] is { Length: > 0 } ? _frames[0][0] : default;
            return _count > 0 && _frames[0] is { Length: > 0 };
        }

        public static Material CreateMaterial()
        {
            // Both games' blade.gbf use SRCALPHA/ONE, no culling or depth writes.
            Shader shader = Shader.Find("Pal3/Effect");
            if (shader == null)
                throw new InvalidOperationException("The weapon blade shader is missing.");
            var material = new Material(shader) { hideFlags = HideFlags.DontSave };
            material.mainTexture = Texture2D.whiteTexture;
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.One);
            material.SetInt("_Cull", (int)CullMode.Off);
            return material;
        }

        // Compatibility sampling path for the PAL3A engine boundary and direct
        // single-update oracles. PAL3 combat calls SamplePose and Advance apart.
        public void Sample(Transform weapon, bool visible, float delta, float rate, float trailSeconds)
        {
            if (_count > 0)
                _lifetime = trailSeconds;
            Advance(delta, rate);
            SamplePose(weapon, visible, trailSeconds);
        }

        // Published draws use separate flush snapshots and can skip the
        // compatibility mesh without changing either update's native clock.
        public void Advance(float delta, float rate, bool rebuildMesh = true)
        {
            if (_created)
                _time = (float)(_time + (double)delta * rate);
            if (_count > 0 && (double)_time - _times[0] > (double)_lifetime * 100)
                _count = 0;
            Array.Clear(_fresh, 0, _fresh.Length);
            if (rebuildMesh)
                Rebuild(_mesh);
        }

        public void SamplePose(Transform weapon, bool visible, float trailSeconds, bool rebuildMesh = true)
        {
            if (_count > 0)
                _lifetime = trailSeconds;
            Array.Clear(_fresh, 0, _fresh.Length);
            if (visible)
            {
                _created = true;
                var current = new Vector3[_tags.Length];
                for (int index = 0; index < current.Length; index++)
                {
                    var matrix = _tags[index].GameBoxTransformMatrix;
                    current[index] = weapon.TransformPoint(new Core.Primitives.GameBoxVector3(matrix.Tx, matrix.Ty, matrix.Tz).ToUnityPosition());
                }
                if (_count == 0 || (double)_time - _times[1] >= (double)_lifetime * (double)0.025f)
                {
                    for (int index = Math.Min(_count, MaximumFrames - 1); index > 1; index--)
                    {
                        _frames[index] = _frames[index - 1];
                        _times[index] = _times[index - 1];
                    }
                    _frames[1] = current;
                    _times[1] = _time;
                    _fresh[1] = true;
                    _count = Math.Clamp(_count + 1, 2, MaximumFrames);
                }
                _frames[0] = current;
                _times[0] = _time;
                _fresh[0] = true;
            }
            if (rebuildMesh)
                Rebuild(_mesh);
        }

        private void Rebuild(Mesh target)
        {
            target.Clear();
            if (_count < 3 || _tags.Length < 2)
                return;
            int vertexCount = _count * _tags.Length;
            int indexCount = (_count - 1) * (_tags.Length - 1) * 6;
            for (int frame = 0; frame < _count; frame++)
            {
                // The published DLL uses _ftol (truncate toward zero),
                // retaining the x87 intermediate until the integer store.
                int alpha = Math.Clamp(100 - (int)(100 * ((double)_time - _times[frame]) / _lifetime), 0, 100);
                for (int index = 0; index < _tags.Length; index++)
                {
                    _vertices[frame * _tags.Length + index] = _frames[frame][index];
                    uint tint = _tags[index].TintColor;
                    _colors[frame * _tags.Length + index] = new Color32((byte)(tint >> 16), (byte)(tint >> 8), (byte)tint, _fresh[frame] ? (byte)(tint >> 24) : (byte)alpha);
                }
            }
            target.SetVertices(_vertices, 0, vertexCount);
            target.SetColors(_colors, 0, vertexCount);
            target.SetTriangles(_triangles, 0, indexCount, 0, false);
            target.RecalculateBounds();
        }

        public void Render(Camera camera, Material material, int layer)
        {
            if (_mesh.vertexCount > 0)
                Graphics.DrawMesh(_mesh, Matrix4x4.identity, material, layer, camera);
        }

        // These two snapshots cannot alias: deferred rendering would otherwise
        // see the second Update's alpha in both queued draws.
        public void RenderFlush(Camera camera, Material material, int layer, int flush, bool rebuild)
        {
            if (_flushMeshes[flush] == null)
            {
                _flushMeshes[flush] = new Mesh { name = "Original blade flush " + flush, hideFlags = HideFlags.HideAndDontSave };
                _flushMeshes[flush].MarkDynamic();
                var root = new GameObject("Original blade flush " + flush) { hideFlags = HideFlags.HideAndDontSave, layer = layer };
                Scene scene = camera.scene.IsValid() ? camera.scene : camera.gameObject.scene;
                SceneManager.MoveGameObjectToScene(root, scene);
                root.AddComponent<MeshFilter>().sharedMesh = _flushMeshes[flush];
                var renderer = root.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.sortingOrder = flush == 0 ? 0 : 3;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                _flushRenderers[flush] = renderer;
                rebuild = true;
            }
            if (rebuild)
                Rebuild(_flushMeshes[flush]);
            _flushRenderers[flush].enabled = _flushMeshes[flush].vertexCount > 0;
        }

        public void Dispose()
        {
            _mesh.Destroy();
            for (int i = 0; i < _flushMeshes.Length; i++)
            {
                if (_flushRenderers[i] != null)
                    _flushRenderers[i].gameObject.Destroy();
                if (_flushMeshes[i] != null)
                    _flushMeshes[i].Destroy();
            }
        }
    }

}
