// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect.Original
{
    using Core.DataReader.Data;
    using UnityEngine;

    internal sealed class OriginalGeometryNode : OriginalEffectNode
    {
        private readonly EffectMeshBuffer _mesh;
        private readonly string _kind;
        private readonly Vector3[] _points;
        private readonly int _columns, _rows;

        public override int QuadCount => _mesh.QuadCount;

        public OriginalGeometryNode(GameObject root, EffectDefinition d, EffectTask[] tasks, Vector3 origin, float birth, OriginalEffectResources resources, string kind)
            : base(root, d, tasks, origin, birth)
        {
            _kind = kind;
            Scale = new Vector3(d.InitSize, 1, d.InitSize);
            _mesh = new EffectMeshBuffer(root, resources.Material(d.TextureName, d.IsAdditiveBlend, d.IsZTestIgnored));
            if (kind == "cylinder")
            {
                _columns = Mathf.Clamp(Mathf.RoundToInt(d.DivisionCount), 1, 4096);
                _rows = 1;
                _points = new Vector3[(_columns + 1) * 2];
                for (int i = 0; i <= _columns; i++)
                {
                    float a = i * d.RotationAngle / _columns;
                    float top = d.TopRadius + d.TopRadiusIncreaseSpeed * a - (i % 2 == 1 ? d.TopShrink : 0);
                    float bottom = d.BottomRadius + d.BottomRadiusIncreaseSpeed * a - (i % 2 == 1 ? d.BottomShrink : 0);
                    float rad = a * Mathf.Deg2Rad;
                    _points[i * 2] = new Vector3(Mathf.Sin(rad) * bottom, d.BottomRiseSpeed * a, Mathf.Cos(rad) * bottom);
                    _points[i * 2 + 1] = new Vector3(Mathf.Sin(rad) * top, d.Width + d.TopRiseSpeed * a, Mathf.Cos(rad) * top);
                }
            }
            else
            {
                _columns = Mathf.Clamp(Mathf.RoundToInt(d.RotationAngle), 1, 128);
                _rows = Mathf.Clamp(Mathf.RoundToInt(d.DivisionCount), 1, 128);
                _points = new Vector3[(_columns + 1) * (_rows + 1)];
                for (int z = 0; z <= _rows; z++)
                    for (int x = 0; x <= _columns; x++)
                        _points[z * (_columns + 1) + x] = new Vector3((x - _columns * 0.5f) * d.Width, 0, (_rows * 0.5f - z) * d.Width);
            }
        }

        protected override void Apply(EffectTask t, OriginalTaskClock clock, float dt)
        {
            base.Apply(t, clock, dt);
            if (_kind == "cylinder")
            {
                Scale.z = Scale.x;
                // The union at CreateTimeSlot is UV speed for cylinders, not
                // Z stretch. GameBox resets a positive V offset after one turn.
                if (TextureOffset > 1)
                    TextureOffset = 0;
            }
            else
            {
                // Grid Z stretch shares CreateTimeSlot; YStretchSpeed is not
                // used by CEffGrid (height comes from its flap calculation).
                Scale.y = 1;
            }
        }

        public override void Render(Camera camera)
        {
            _mesh.Clear();
            if (!Alive || !Visible)
            {
                _mesh.Upload();
                return;
            }
            var uv = Atlas(LocalTime);
            Quaternion rot = RawRotation(Rotation);
            Vector3 P(int index)
            {
                Vector3 p = _points[index];
                if (_kind == "grid")
                {
                    float x = index % (_columns + 1), z = index / (_columns + 1);
                    float sin = Mathf.Sin((LocalTime + x + 1) * Definition.BottomRadius + z + 1);
                    float cos = Mathf.Cos((LocalTime + z + 1) * Definition.BottomRadius + x + 1);
                    p.x += (sin - cos) * Definition.TopRadius;
                    p.z += (cos + sin) * Definition.TopRadius;
                    p.y += Mathf.Sin(LocalTime * Definition.TopRadiusIncreaseSpeed + x) * Definition.TextureRepeat;
                }
                return UnityPosition(Position + Orbit + rot * Vector3.Scale(p, Scale));
            }
            if (_kind == "cylinder")
            {
                float repeat = Definition.TextureRepeat;
                for (int i = 0; i < _columns; i++)
                {
                    float u0 = uv.min.x + (uv.max.x - uv.min.x) * repeat * i / _columns;
                    float u1 = uv.min.x + (uv.max.x - uv.min.x) * repeat * (i + 1) / _columns;
                    _mesh.Quad(P(i * 2), P(i * 2 + 2), P(i * 2 + 3), P(i * 2 + 1),
                        new Vector2(u0, uv.min.y + TextureOffset), new Vector2(u1, uv.max.y + TextureOffset), Tint);
                }
            }
            else
            {
                for (int z = 0; z < _rows; z++)
                    for (int x = 0; x < _columns; x++)
                    {
                        int i = z * (_columns + 1) + x;
                        Vector2 a = Vector2.Lerp(uv.min, uv.max, 0);
                        a.x = Mathf.Lerp(uv.min.x, uv.max.x, (float)x / _columns);
                        a.y = Mathf.Lerp(uv.min.y, uv.max.y, (float)z / _rows);
                        Vector2 b = new(Mathf.Lerp(uv.min.x, uv.max.x, (float)(x + 1) / _columns), Mathf.Lerp(uv.min.y, uv.max.y, (float)(z + 1) / _rows));
                        _mesh.Quad(P(i), P(i + 1), P(i + _columns + 2), P(i + _columns + 1), a, b, Tint);
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
