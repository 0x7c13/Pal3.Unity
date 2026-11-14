// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect.Original
{
    using System;
    using System.IO;
    using Core.DataReader.Par;
    using Core.Primitives;
    using UnityEngine;

    // PAL3A 0x4c8509 / 0x4c8446 / 0x4c8bfc. Keep the native update order,
    // including newly emitted units receiving a full update in their birth frame.
    internal sealed class OriginalSparEmitter
    {
        internal struct Unit
        {
            public Vector3 Position, Velocity, Color;
            public float Age, Life, Size, Alpha, Rotation, Spin;
            public int Frame;
        }

        private readonly SparGroup _group;
        private readonly Vector3[] _shapePoints, _pathPoints;
        private readonly float[] _controllerAngles;
        private uint _random;
        private float _time, _emissionFraction, _pathFraction;
        private int _keyIndex, _previousKey = -1, _pathIndex;
        private Vector3 _position, _boxMinimum, _boxMaximum;
        private int _shapeCount, _pathCount;

        public Unit[] Units
        {
            get;
        }

        public int Count
        {
            get; private set;
        }

        public float Time => _time;

        public OriginalSparEmitter(SparGroup group, uint seed)
        {
            Validate(group);
            _group = group;
            _random = seed;
            Units = new Unit[group.Capacity];
            int shapes = 0, paths = 0;
            foreach (SparKeyframe k in group.Keyframes)
            {
                shapes = Math.Max(shapes, k.Shape == 3 ? k.ShapeParameters.Length / 3 : 0);
                paths = Math.Max(paths, k.PathPoints.Length);
            }
            _shapePoints = new Vector3[shapes];
            _pathPoints = new Vector3[paths];
            _controllerAngles = new float[group.Keyframes.Length];
        }

        public void Start()
        {
            Count = 0;
            _time = 0;
        }

        public void Stop()
        {
            Count = 0;
            _time = 0;
            _keyIndex = 0;
            _emissionFraction = 0;
            _pathIndex = 0;
            _pathFraction = 0;
        }

        public static void Validate(SparGroup group)
        {
            if (group.Mode != 0)
                throw new NotSupportedException($"SPar emitter mode {group.Mode}");
            if (group.Keyframes.Length == 0 || group.TextureFrames.Length == 0)
                throw new InvalidDataException("SPar requires keyframes and texture frames");
            float previous = float.NegativeInfinity;
            foreach (SparKeyframe k in group.Keyframes)
            {
                if (!float.IsFinite(k.Time) || k.Time < previous)
                    throw new InvalidDataException("SPar keyframe times must be finite and ordered");
                previous = k.Time;
                if (k.Shape == 3 && k.ShapeParameters.Length == 0 || k.HasPath && k.PathPoints.Length == 0)
                    throw new InvalidDataException("SPar point shape/path is empty");
                for (int i = 0; i < 6; i++)
                {
                    int maximum = i switch
                    {
                        0 => 6,
                        1 => 1,
                        2 => 2,
                        3 => 2,
                        4 => 2,
                        _ => 1
                    };
                    if (k.Controllers[i].Kind < 0 || k.Controllers[i].Kind > maximum)
                        throw new NotSupportedException($"SPar controller {i}:{k.Controllers[i].Kind}");
                }
                if (k.Controllers[4].Kind != 0 && k.Controllers[4].Parameters[0] <= 0)
                    throw new InvalidDataException("SPar frame interval must be positive");
            }
        }

        private float Random()
        {
            // MSVCRT rand(), PAL3A 0x540f34, inclusive 0..32767.
            _random = unchecked(_random * 214013 + 2531011);
            return (_random >> 16 & 32767) / 32767f;
        }

        private float Between(float a, float b) => a + Random() * (b - a);

        private Vector3 Between(Vector3 a, Vector3 b) => new(Between(a.x, b.x), Between(a.y, b.y), Between(a.z, b.z));

        private static Vector3 V(GameBoxVector3 v) => new(v.X, v.Y, v.Z);

        private static float L(float a, float b, float t) => a + (b - a) * t;

        private static Vector3 V(float[] values, int index) => new(values[index], values[index + 1], values[index + 2]);

        public void Step(float dt)
        {
            _time += dt;
            SparKeyframe[] keys = _group.Keyframes;
            if (_time > keys[^1].Time)
            {
                _time = _group.Loop ? 0 : keys[^1].Time;
                _keyIndex = _group.Loop ? 0 : keys.Length - 1;
            }
            while (_keyIndex + 1 < keys.Length && _time > keys[_keyIndex + 1].Time)
                _keyIndex++;
            SparKeyframe a = keys[_keyIndex], b = keys[Math.Min(_keyIndex + 1, keys.Length - 1)];
            float t = Mathf.Clamp01(Mathf.Max(0, _time - a.Time) / (a.Time == b.Time ? 1 : b.Time - a.Time));
            SamplePoints(a, b, t, dt);

            // The original loader overwrites the authored rates with 500/600
            // (0x4c693a). Capacity is still the file's authored particle limit.
            int rate = 500 + (int)(Random() * 100);
            float emission = rate * dt;
            int births = (int)emission;
            _emissionFraction += emission - births;
            if (_emissionFraction >= 1)
            {
                births++;
                _emissionFraction--;
            }
            for (int i = 0; i < births && Count < Units.Length; i++)
                Units[Count++] = Spawn(a, b, t);
            _previousKey = _keyIndex;

            for (int i = 0; i < Count; i++)
            {
                ref Unit u = ref Units[i];
                u.Age += dt;
                if (u.Age >= u.Life)
                {
                    // Native removal swaps the last unit and skips updating it
                    // again in this frame (0x4c8c17, 0x4c8cb3).
                    Units[i] = Units[--Count];
                    continue;
                }
                UpdateUnit(ref u, a.Controllers, dt);
            }
        }

        private void SamplePoints(SparKeyframe a, SparKeyframe b, float t, float dt)
        {
            if (a.Shape == 2 && _previousKey != _keyIndex)
            {
                _boxMinimum = V(a.ShapeParameters, 0);
                _boxMaximum = V(a.ShapeParameters, 3);
            }
            if (a.Shape == 3)
            {
                int left = a.ShapeParameters.Length / 3;
                if (b.Shape == 3)
                {
                    int right = b.ShapeParameters.Length / 3;
                    _shapeCount = (int)L(left, right, t);
                    for (int i = 0; i < _shapeCount; i++)
                        _shapePoints[i] = Vector3.LerpUnclamped(V(a.ShapeParameters, i * left / _shapeCount * 3),
                            V(b.ShapeParameters, i * right / _shapeCount * 3), t);
                }
                else if (_previousKey != _keyIndex)
                {
                    _shapeCount = left;
                    for (int i = 0; i < left; i++)
                        _shapePoints[i] = V(a.ShapeParameters, i * 3);
                }
            }
            if (a.HasPath)
            {
                int left = a.PathPoints.Length;
                if (b.HasPath)
                {
                    int right = b.PathPoints.Length;
                    _pathCount = (int)L(left, right, t);
                    for (int i = 0; i < _pathCount; i++)
                        _pathPoints[i] = Vector3.LerpUnclamped(V(a.PathPoints[i * left / _pathCount]), V(b.PathPoints[i * right / _pathCount]), t);
                }
                else if (_previousKey != _keyIndex)
                {
                    _pathCount = left;
                    for (int i = 0; i < left; i++)
                        _pathPoints[i] = V(a.PathPoints[i]);
                }
                float step = dt * (b.HasPath ? L(a.PathRate, b.PathRate, t) : a.PathRate);
                _pathIndex += (int)step;
                _pathFraction += step - (int)step;
                if (_pathFraction >= 1)
                {
                    _pathIndex++;
                    _pathFraction--;
                }
                if (_pathIndex >= _pathCount)
                    _pathIndex = 0;
                _position = _pathPoints[_pathIndex];
            }
            // Native interpolation does not copy authored Position. The live
            // emitter starts at zero until its path moves.
            SparController controller = a.Controllers[5];
            if (a.Shape == 3 && controller.Kind == 1)
            {
                float[] p = controller.Parameters;
                _controllerAngles[_keyIndex] += dt * p[3];
                Vector3 axis = V(p, 0);
                float angle = _controllerAngles[_keyIndex];
                float sin = Mathf.Sin(angle), cos = Mathf.Cos(angle);
                for (int i = 0; i < _shapeCount; i++)
                {
                    Vector3 v = _shapePoints[i];
                    // Rodrigues' matrix uses the authored axis without normalizing.
                    _shapePoints[i] = cos * v + sin * Vector3.Cross(axis, v) + (1 - cos) * Vector3.Dot(axis, v) * axis;
                }
            }
        }

        private Unit Spawn(SparKeyframe a, SparKeyframe b, float t)
        {
            var u = new Unit
            {
                Alpha = Between(L(a.AlphaMinimum, b.AlphaMinimum, t), L(a.AlphaMaximum, b.AlphaMaximum, t)),
                Life = Between(L(a.LifeMinimum, b.LifeMinimum, t), L(a.LifeMaximum, b.LifeMaximum, t)),
                Size = Between(L(a.SizeMinimum, b.SizeMinimum, t), L(a.SizeMaximum, b.SizeMaximum, t)),
                Color = Between(Vector3.LerpUnclamped(V(a.ColorMinimum), V(b.ColorMinimum), t), Vector3.LerpUnclamped(V(a.ColorMaximum), V(b.ColorMaximum), t))
            };
            Random();
            Random();
            u.Spin = Between(L(a.SpinMinimum, b.SpinMinimum, t), L(a.SpinMaximum, b.SpinMaximum, t));
            u.Velocity = Between(Vector3.LerpUnclamped(V(a.VelocityMinimum), V(b.VelocityMinimum), t), Vector3.LerpUnclamped(V(a.VelocityMaximum), V(b.VelocityMaximum), t));
            Random();
            Random();
            u.Rotation = Between(L(a.RotationMinimum, b.RotationMinimum, t), L(a.RotationMaximum, b.RotationMaximum, t));
            // The loader reads frame bounds into keyframes (+cc/+d0), but
            // 0x4c8509 never copies them into the separate live state, cleared
            // by 0x4c5efd. Spawn 0x4c625e still consumes rand() for its 0..0 range.
            Random();
            u.Position = _position;
            switch (a.Shape)
            {
                case 1:
                    float radius = Random() * L(a.ShapeParameters[0], b.Shape == 1 ? b.ShapeParameters[0] : 0, t);
                    float angle = Random() * (2 * Mathf.PI);
                    u.Position += new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                    break;
                case 2:
                    u.Position += Between(_boxMinimum, _boxMaximum);
                    break;
                case 3:
                    if (_shapeCount > 0)
                        u.Position += _shapePoints[Math.Min((int)(Random() * _shapeCount), _shapeCount - 1)];
                    break;
            }
            return u;
        }

        private void UpdateUnit(ref Unit u, SparController[] controllers, float dt)
        {
            SparController motion = controllers[0];
            int kind = motion.Kind;
            if (kind is 2 or 3)
            {
                u.Velocity += V(motion.Parameters, 0) * dt;
                if (kind == 3)
                {
                    float speed = u.Velocity.magnitude;
                    u.Velocity = speed > 0 ? u.Velocity / speed * Mathf.Max(0, speed - u.Size * motion.Parameters[3] * speed * dt) : Vector3.zero;
                }
            }
            if (kind is >= 1 and <= 3)
                u.Position += u.Velocity * dt;
            else if (kind >= 4)
            {
                float speed = u.Velocity.magnitude;
                if (kind == 5)
                    speed *= 1 - u.Age / u.Life;
                else if (kind == 6)
                    speed *= u.Age / u.Life;
                u.Position += (_position - u.Position).normalized * speed * dt;
            }
            if (controllers[1].Kind == 1)
                u.Rotation += u.Spin * dt;
            if (controllers[2].Kind == 1)
            {
                u.Color += new Vector3(Random() * 2 - 1, Random() * 2 - 1, Random() * 2 - 1) * dt;
                u.Color = new Vector3(Mathf.Clamp01(u.Color.x), Mathf.Clamp01(u.Color.y), Mathf.Clamp01(u.Color.z));
            }
            else if (controllers[2].Kind == 2)
                u.Color = Vector3.LerpUnclamped(V(controllers[2].Parameters, 0), V(controllers[2].Parameters, 3), u.Age / u.Life);
            if (controllers[3].Kind == 1)
                u.Alpha = 1 - u.Age / u.Life;
            else if (controllers[3].Kind == 2)
                u.Alpha = u.Age / u.Life;
            if (controllers[4].Kind != 0)
            {
                int frame = (int)(u.Age / controllers[4].Parameters[0]);
                u.Frame = controllers[4].Kind == 1 ? Math.Min(frame, _group.TextureFrames.Length - 1) : frame % _group.TextureFrames.Length;
            }
        }
    }
}
