// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect.Original
{
    using System;
    using Core.DataReader.Data;
    using UnityEngine;

    // CEffExplosive: subdivided plane, per-fragment velocity and spin.
    internal sealed class OriginalExplosiveNode : OriginalEffectNode
    {
        private readonly EffectMeshBuffer _mesh;
        private readonly Vector3[][] _corners;
        private readonly Vector3[] _velocity, _spin;
        private readonly bool[] _exploded;
        private readonly System.Random _random;
        private readonly int _columns, _rows;

        public override int QuadCount => _mesh.QuadCount;

        public OriginalExplosiveNode(GameObject root, EffectDefinition d, EffectTask[] tasks, Vector3 origin, float birth, OriginalEffectResources resources, uint seed)
            : base(root, d, tasks, origin, birth)
        {
            _columns = (int)d.RotationAngle;
            _rows = (int)d.DivisionCount;
            if (_columns < 1 || _rows < 1 || (long)_columns * _rows >= 2501)
                throw new InvalidOperationException("Invalid original explosive subdivisions");
            _mesh = new EffectMeshBuffer(root, resources.Material(d.TextureName, d.IsAdditiveBlend, d.IsZTestIgnored));
            _random = new System.Random(unchecked((int)seed));
            _corners = new Vector3[_columns * _rows][];
            _velocity = new Vector3[_corners.Length];
            _spin = new Vector3[_corners.Length];
            _exploded = new bool[Tasks.Length];
            for (int row = 0; row < _rows; row++)
                for (int column = 0; column < _columns; column++)
                {
                    float x = (column - _columns / 2f) * d.Width;
                    float z = (_rows / 2f - row) * d.Width;
                    _corners[row * _columns + column] = new[]
                    {
                        new Vector3(x, 0, z), new Vector3(x + d.Width, 0, z),
                        new Vector3(x + d.Width, 0, z - d.Width), new Vector3(x, 0, z - d.Width)
                    };
                }
        }

        private void Explode()
        {
            int maximum = Mathf.RoundToInt(Definition.TopRadius);
            if (maximum <= 0)
                maximum = 2;
            float minimum = Mathf.Max(0, Definition.BottomRadius);
            int count = 0;
            // Preserve the original column/row traversal, including its center indices.
            for (int column = 0; column < _columns; column++)
                for (int row = 0; row < _rows; row++, count++)
                {
                    int power = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(minimum, maximum -
                        Mathf.Abs(row - _columns / 2) * Mathf.Abs(column - _rows / 2) * Mathf.Max(0, Definition.TopRiseSpeed))));
                    float Component() => _random.Next(power) - power / 2;
                    Vector3 velocity = new(Component(), Component(), Component());
                    if (velocity.magnitude < minimum)
                        velocity = velocity.normalized * minimum;
                    _velocity[count] = velocity + new Vector3(Definition.TopRadiusIncreaseSpeed, Definition.BottomRadiusIncreaseSpeed, Definition.TextureRepeat);
                    float Spin() => (_random.Next(160) - 80) * 16f;
                    _spin[count] = new Vector3(Spin(), Spin(), Spin());
                }
        }

        protected override void Apply(EffectTask t, OriginalTaskClock clock, float dt)
        {
            Position += new Vector3(t.SpeedX, t.SpeedY, t.SpeedZ) * dt;
            Rotation += new Vector3(t.RotAxisX, t.RotAxisY, t.RotAxisZ) * dt;
            Scale += new Vector3(t.XStretchSpeed, 0, t.CreateTimeSlot) * dt;
            Tint.a = Mathf.Clamp01(Tint.a + t.SpeedAlpha * dt / 255);
            Orbit = OrbitPosition(new Vector3(t.CircleAxisX, t.CircleAxisY, t.CircleAxisZ), (LocalTime - clock.StartedAt) * t.CircleSpeed, t.CircleRadius);
            int index = Array.IndexOf(Clocks, clock);
            if (LocalTime - clock.StartedAt <= t.SpeedRed)
                return;
            if (!_exploded[index])
            {
                Explode();
                _exploded[index] = true;
            }
            for (int i = 0; i < _corners.Length; i++)
            {
                Vector3[] corners = _corners[i];
                Vector3 center = (corners[0] + corners[1] + corners[2] + corners[3]) / 4;
                Vector3 moved = center + _velocity[i] * dt;
                Quaternion rotation = Quaternion.AngleAxis(_spin[i].y * dt, Vector3.right) *
                    Quaternion.AngleAxis(_spin[i].x * dt, Vector3.up) * Quaternion.AngleAxis(_spin[i].z * dt, Vector3.forward);
                for (int corner = 0; corner < 4; corner++)
                    corners[corner] = moved + rotation * (corners[corner] - center);
            }
        }

        public override void Render(Camera camera)
        {
            _mesh.Clear();
            if (Alive && Visible)
            {
                var atlas = Atlas(LocalTime);
                Vector2 step = (atlas.max - atlas.min) / new Vector2(_columns, _rows);
                Quaternion rotation = RawRotation(Rotation);
                for (int i = 0; i < _corners.Length; i++)
                {
                    Vector3 P(int corner)
                    {
                        Vector3 v = rotation * _corners[i][corner];
                        v = Vector3.Scale(v, new Vector3(Scale.x, 1, Scale.z));
                        return UnityPosition(Position + Orbit + v);
                    }
                    Vector2 uv = atlas.min + Vector2.Scale(new Vector2(i % _columns, i / _columns), step);
                    _mesh.Quad(P(0), P(1), P(2), P(3), uv, uv + step, Tint);
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
