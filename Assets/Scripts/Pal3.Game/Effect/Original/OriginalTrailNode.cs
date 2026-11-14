// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect.Original
{
    using System;
    using System.Collections.Generic;
    using Core.DataReader.Data;
    using UnityEngine;

    internal sealed class OriginalTrailNode : OriginalEffectNode
    {
        private readonly EffectMeshBuffer _mesh, _trailMesh;
        private readonly string _kind;
        private readonly System.Random _random;
        private readonly Vector3 _origin;
        private readonly List<Vector3>[] _trails;
        private readonly Vector3[] _positions, _velocities;
        private readonly bool[] _launched;
        private float _meteorTime, _lum;
        private readonly float[] _shockFrame, _shockLimit;
        private readonly int _shockNodes, _cascades;

        public override int QuadCount => _mesh.QuadCount + (_trailMesh?.QuadCount ?? 0);

        public OriginalTrailNode(GameObject root, EffectDefinition d, EffectTask[] tasks, Vector3 origin, float birth,
            OriginalEffectResources resources, string kind, uint seed) : base(root, d, tasks, origin, birth)
        {
            _kind = kind;
            _origin = origin;
            _random = new System.Random(unchecked((int)seed));
            _mesh = new EffectMeshBuffer(root, resources.Material(d.TextureName, d.IsAdditiveBlend, d.IsZTestIgnored));
            _launched = new bool[Tasks.Length];
            if (kind == "meteor")
            {
                var child = new GameObject("Original meteor chain") { hideFlags = HideFlags.DontSave, layer = root.layer };
                child.transform.SetParent(root.transform, false);
                // CMeteorChain always uses depth-tested additive blending. When
                // the meteor has a textured head, its ribbon is untextured.
                _trailMesh = new EffectMeshBuffer(child, resources.Material(d.RotationAngle > 0 ? null : d.TextureName, true, false));
                _lum = d.TextureRepeat;
                int count = Mathf.Clamp(Mathf.Abs(Mathf.RoundToInt(d.RotationAngle)), 1, 512);
                _trails = new List<Vector3>[count];
                _positions = new Vector3[count];
                _velocities = new Vector3[count];
                for (int i = 0; i < count; i++)
                    _trails[i] = new List<Vector3>();
            }
            else
            {
                _shockNodes = Mathf.Clamp(Mathf.RoundToInt(d.RotationAngle), 1, 512);
                _cascades = Mathf.Clamp(Mathf.RoundToInt(d.Width), 1, 64);
                _shockFrame = new float[_shockNodes * _cascades];
                _shockLimit = new float[_shockFrame.Length];
            }
        }

        protected override void Apply(EffectTask t, OriginalTaskClock clock, float dt)
        {
            int task = Array.IndexOf(Tasks, t);
            if (_kind == "shock")
            {
                if (clock.StartedNow)
                    for (int i = 0; i < _shockFrame.Length; i++)
                    {
                        _shockFrame[i] = (float)_random.NextDouble() * Mathf.Max(1, Definition.FrameCount);
                        _shockLimit[i] = t.RequireLoops + _shockFrame[i] + (i % _cascades) * Definition.BottomRadius;
                    }
                return; // Original shock tasks only trigger the animated plane stack.
            }
            Vector3 launchPosition = Position;
            base.Apply(t, clock, dt);
            _lum = Mathf.Clamp(_lum + t.SpeedAlpha * dt, 0, 255);
            if (!_launched[task] && LocalTime - clock.StartedAt >= t.SpeedRed)
            {
                _launched[task] = true;
                for (int i = 0; i < _trails.Length; i++)
                {
                    _trails[i].Clear();
                    _positions[i] = launchPosition;
                    float a = i * 2 * Mathf.PI / _trails.Length;
                    _velocities[i] = new Vector3(Definition.BottomRiseSpeed,
                        _trails.Length == 1 ? Definition.TopShrink : Mathf.Sin(a) * Definition.TopShrink,
                        _trails.Length == 1 ? Definition.BottomShrink : Mathf.Cos(a) * Definition.BottomShrink);
                }
            }
        }

        public override void Step(float time, float dt)
        {
            base.Step(time, dt);
            if (_kind == "shock")
            {
                for (int i = 0; i < _shockFrame.Length; i++)
                    _shockFrame[i] += Definition.FramePerSecond * dt;
                return;
            }
            bool running = false;
            for (int i = 0; i < _launched.Length; i++)
                running |= _launched[i] && !Clocks[i].Finished;
            if (!running)
                return;
            _meteorTime += dt;
            Vector3 target = _origin + new Vector3(Definition.TopRadiusIncreaseSpeed, Definition.BottomRadiusIncreaseSpeed, Definition.TopRiseSpeed);
            while (_meteorTime + 0.000001f >= 0.02f)
            {
                _meteorTime -= 0.02f;
                for (int i = 0; i < _trails.Length; i++)
                {
                    _velocities[i] += (target - _positions[i]) * 0.02f;
                    _positions[i] += _velocities[i] * 0.02f;
                    _trails[i].Insert(0, _positions[i]);
                    int capacity = Mathf.Clamp(Mathf.RoundToInt(Definition.DivisionCount), 2, 4096);
                    if (_trails[i].Count > capacity)
                        _trails[i].RemoveAt(_trails[i].Count - 1);
                }
            }
        }

        public override void Render(Camera camera)
        {
            _mesh.Clear();
            _trailMesh?.Clear();
            if (!Alive || !Visible)
            {
                _mesh.Upload();
                _trailMesh?.Upload();
                return;
            }
            if (_kind == "shock")
            {
                Quaternion rot = RawRotation(Rotation);
                for (int i = 0; i < _shockFrame.Length; i++)
                {
                    if (_shockFrame[i] > Mathf.Max(1, Definition.FrameCount) * _shockLimit[i])
                        continue;
                    int node = i / _cascades;
                    float h = Definition.TopRadius;
                    float top = h + node * (h - Definition.TextureRepeat), half = Definition.DivisionCount / 2;
                    Vector3 P(float x, float y) => UnityPosition(Position + rot * (new Vector3(x, y, 0) * Definition.InitSize));
                    var uv = Atlas(Definition.FramePerSecond == 0 ? 0 : _shockFrame[i] / Definition.FramePerSecond);
                    _mesh.Quad(P(-half, top), P(half, top), P(half, top - h), P(-half, top - h), uv.min, uv.max, Tint);
                }
            }
            else
            {
                Vector3 cameraPosition = camera != null ? Root.transform.InverseTransformPoint(camera.transform.position) : new Vector3(0, 0, -10);
                for (int chain = 0; chain < _trails.Length; chain++)
                {
                    List<Vector3> nodes = _trails[chain];
                    for (int i = 0; i + 1 < nodes.Count; i++)
                    {
                        float age = i * 0.02f;
                        float width = Definition.Width - Definition.BottomRadius * age;
                        float brightness = _lum - Definition.TopRadius * age;
                        if (width <= 0 || brightness <= 0)
                            break;
                        Vector3 a = UnityPosition(nodes[i]), b = UnityPosition(nodes[i + 1]);
                        Vector3 side = Vector3.Cross(a - b, a - cameraPosition).normalized * (width / 40);
                        // GameBox renders a strip with a separate width, camera
                        // cross vector and luminosity at each chain node. Share
                        // those endpoints to avoid cracks between tapered quads.
                        Vector3 endSide = i + 2 < nodes.Count
                            ? Vector3.Cross(b - UnityPosition(nodes[i + 2]), b - cameraPosition).normalized
                                * (Mathf.Max(0, Definition.Width - Definition.BottomRadius * (age + 0.02f)) / 40)
                            : side;
                        Color color = Tint;
                        color.a = Mathf.Clamp01(brightness / 255);
                        Color endColor = Tint;
                        endColor.a = Mathf.Clamp01((_lum - Definition.TopRadius * (age + 0.02f)) / 255);
                        _trailMesh.Quad(a - side, b - endSide, b + endSide, a + side, new Vector2((float)i / nodes.Count, 0),
                            new Vector2((float)(i + 1) / nodes.Count, 1), color, endColor, endColor, color);
                    }
                    if (Definition.RotationAngle > 0 && nodes.Count > 0)
                    {
                        Vector3 center = UnityPosition(nodes[0]);
                        Vector3 right = camera != null ? Root.transform.InverseTransformVector(camera.transform.right) : Vector3.right;
                        Vector3 up = camera != null ? Root.transform.InverseTransformVector(camera.transform.up) : Vector3.up;
                        var spin = Quaternion.AngleAxis(-Rotation.z, Vector3.Cross(right, up));
                        right = spin * right * Scale.x / 20;
                        up = spin * up * Scale.z / 20;
                        var uv = Atlas(LocalTime);
                        _mesh.Quad(center - right - up, center + right - up, center + right + up, center - right + up, uv.min, uv.max, Tint);
                    }
                }
            }
            _mesh.Upload();
            _trailMesh?.Upload();
        }

        public override void Dispose()
        {
            _mesh.Dispose();
            _trailMesh?.Dispose();
            base.Dispose();
        }
    }
}
