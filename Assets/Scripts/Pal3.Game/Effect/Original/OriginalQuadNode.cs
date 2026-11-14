// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect.Original
{
    using System.Collections.Generic;
    using Core.DataReader.Data;
    using UnityEngine;

    internal sealed class OriginalQuadNode : OriginalEffectNode
    {
        private sealed class Quad
        {
            public Vector3 Position, Rotation, Orbit;
            public Vector2 Size;
            public Color Color;
            public float Age;

            public Quad Copy() => new() { Position = Position, Rotation = Rotation, Orbit = Orbit, Size = Size, Color = Color };
        }

        private readonly List<Quad> _quads = new();
        private readonly EffectMeshBuffer _mesh;
        private readonly float[] _spawnAt, _lastSpawn;
        private readonly int _loops;

        public override int QuadCount => _mesh.QuadCount;

        public OriginalQuadNode(GameObject root, EffectDefinition d, EffectTask[] tasks, Vector3 origin, float birth, OriginalEffectResources resources)
            : base(root, d, tasks, origin, birth)
        {
            _mesh = new EffectMeshBuffer(root, resources.Material(d.TextureName, d.IsAdditiveBlend, d.IsZTestIgnored));
            _spawnAt = new float[Tasks.Length];
            _lastSpawn = new float[Tasks.Length];
            for (int i = 0; i < Tasks.Length; i++)
            {
                _spawnAt[i] = -1;
                _loops = Tasks[i].RequireLoops;
            }
        }

        public override void Step(float time, float dt)
        {
            foreach (Quad q in _quads)
                q.Age += dt;
            if (Definition.FramePerSecond > 0)
                _quads.RemoveAll(q => q.Age * Definition.FramePerSecond >= Mathf.Max(1, Definition.FrameCount) * Mathf.Max(1, _loops));
            base.Step(time, dt);
            bool emitting = false;
            for (int i = 0; i < Tasks.Length; i++)
            {
                if (Clocks[i].StartedAt < 0)
                {
                    emitting |= !Clocks[i].Finished;
                    continue;
                }
                EffectTask t = Tasks[i];
                if (_spawnAt[i] < 0 && LocalTime > Clocks[i].StartedAt + 0.000001f)
                {
                    Spawn();
                    _spawnAt[i] = _lastSpawn[i] = LocalTime;
                }
                if (_spawnAt[i] < 0)
                {
                    emitting = true;
                    continue;
                }
                if (t.CreateNew && LocalTime - _spawnAt[i] <= t.Duration + 0.000001f)
                {
                    if (LocalTime - _lastSpawn[i] >= t.CreateTimeSlot)
                    {
                        Spawn();
                        _lastSpawn[i] = LocalTime;
                    }
                    emitting = true;
                }
            }
            Alive |= emitting;
            if (!Alive)
                _quads.Clear();
        }

        private void Spawn()
        {
            if (_quads.Count >= Mathf.Clamp(Definition.MaxQuadCount, 1, 1024))
                return;
            _quads.Add(_quads.Count > 0 ? _quads[^1].Copy() : new Quad
            {
                Position = Position,
                Rotation = Rotation,
                Size = Vector2.one * Definition.InitSize,
                Color = Tint
            });
        }

        protected override void Apply(EffectTask t, OriginalTaskClock clock, float dt)
        {
            int start = t.ModifyLast ? Mathf.Max(0, _quads.Count - 1) : 0;
            Vector3 axis = new(t.CircleAxisX, t.CircleAxisY, t.CircleAxisZ);
            Vector3 orbit = OrbitPosition(axis, (LocalTime - clock.StartedAt) * t.CircleSpeed, t.CircleRadius);
            for (int i = start; i < _quads.Count; i++)
            {
                Quad q = _quads[i];
                q.Size += new Vector2(t.XStretchSpeed, t.YStretchSpeed) * dt;
                q.Position += new Vector3(t.SpeedX, t.SpeedY, t.SpeedZ) * dt;
                q.Rotation += new Vector3(t.RotAxisX, t.SpeedRot + t.RotAxisY, t.RotAxisZ) * dt;
                q.Color = ChangedColor(q.Color, t, dt);
                q.Orbit = orbit;
            }
        }

        public override void Render(Camera camera)
        {
            _mesh.Clear();
            foreach (Quad q in _quads)
            {
                var uv = Atlas(q.Age);
                Vector3 center = UnityPosition(q.Position + q.Orbit);
                if (Definition.IsBillboard)
                {
                    Vector3 right = camera != null ? Root.transform.InverseTransformVector(camera.transform.right) : Vector3.right;
                    Vector3 up = camera != null ? Root.transform.InverseTransformVector(camera.transform.up) : Vector3.up;
                    Vector3 forward = Vector3.Cross(right, up).normalized;
                    Quaternion spin = Quaternion.AngleAxis(-q.Rotation.y, forward);
                    right = spin * right * (q.Size.x / 20);
                    up = spin * up * (q.Size.y / 20);
                    _mesh.Quad(center - right - up, center + right - up, center + right + up, center - right + up, uv.min, uv.max, q.Color);
                }
                else
                {
                    Quaternion r = RawRotation(q.Rotation);
                    Vector3 P(float x, float z) => UnityPosition(q.Position + q.Orbit + r * new Vector3(x, 0, z));
                    _mesh.Quad(P(-q.Size.x, q.Size.y), P(q.Size.x, q.Size.y), P(q.Size.x, -q.Size.y), P(-q.Size.x, -q.Size.y), uv.min, uv.max, q.Color);
                }
            }
            _mesh.Upload();
        }

        public override void Dispose()
        {
            _mesh.Dispose();
            base.Dispose();
        }
    }
}
