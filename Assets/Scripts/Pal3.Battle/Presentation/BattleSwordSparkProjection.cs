// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle.Presentation
{
    using System;
    using System.Collections.Generic;

    public readonly struct BattleSwordSparkFrame
    {
        public float X
        {
            get;
        }

        public float Y
        {
            get;
        }

        public float Size
        {
            get;
        }

        public byte Alpha
        {
            get;
        }

        public BattleSwordSparkFrame(float x, float y, float size, byte alpha)
        {
            X = x;
            Y = y;
            Size = size;
            Alpha = alpha;
        }
    }

    // PAL3 C2DSpark::CreateStars/Update. The clock and C-runtime random stream
    // belong to this decoration, separately from battle and effect randomness.
    public sealed class BattleSwordSparkProjection
    {
        private sealed class Spark
        {
            public float X, Y, VelocityY, AccelerationY, Luminosity, Size;

            public bool Alive => X < 800 && X > -Size && Y < 600 && Y > -Size &&
                Luminosity > 1 && Luminosity <= 255 && Size >= .01f;

            public void Advance(float seconds)
            {
                VelocityY += seconds * AccelerationY;
                Y += seconds * VelocityY;
                Luminosity -= seconds * 100;
            }
        }

        private readonly List<Spark> _sparks = new();
        private readonly BattleRandom _random;
        private float _lastEmission;

        public double Time
        {
            get; private set;
        }

        public IReadOnlyList<BattleSwordSparkFrame> Frames
        {
            get
            {
                var frames = new BattleSwordSparkFrame[_sparks.Count];
                for (int index = 0; index < frames.Length; index++)
                {
                    Spark spark = _sparks[index];
                    byte alpha = (byte)Math.Clamp((int)Math.Round(spark.Luminosity, MidpointRounding.ToEven), 0, 255);
                    frames[index] = new BattleSwordSparkFrame(spark.X, spark.Y, spark.Size, alpha);
                }
                return Array.AsReadOnly(frames);
            }
        }

        public BattleSwordSparkProjection(int seed) => _random = new BattleRandom(seed);

        public void Advance(double seconds, bool emit, float energy)
        {
            if (!double.IsFinite(seconds) || seconds < 0 || !float.IsFinite(energy) || energy < 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            if (seconds == 0)
                return;
            Time += seconds;
            float now = (float)Time;
            // CCBRoleState stores GetTime() as float, uses a strict boundary,
            // and emits once per pass rather than catching up a stalled frame.
            if (emit && now - _lastEmission > .1f)
            {
                Emit(energy);
                if (energy > 199.9f)
                    Emit(energy);
                _lastEmission = now;
            }
            // RoleState creates the stars before CCBSystem updates C2DSpark.
            foreach (Spark spark in _sparks)
                spark.Advance((float)seconds);
            _sparks.RemoveAll(spark => !spark.Alive);
        }

        private void Emit(float energy)
        {
            float size = energy / 10;
            var spark = new Spark
            {
                Size = size,
                VelocityY = -10,
                Luminosity = 250 - _random.Next() % 50,
                X = 25 - size / 2 - 10 + _random.Next() % 20,
                Y = 570 - size / 2 - 20 + _random.Next() % 40,
                AccelerationY = -(_random.Next() % (size < 19 ? 50 : 100))
            };
            if (_sparks.Count < 512)
                _sparks.Add(spark);
        }

        public void Clear() => _sparks.Clear();
    }
}
