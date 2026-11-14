// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Par
{
    using System;
    using Primitives;

    public sealed class ParticleCurve
    {
        public int Channels;
        public string Name;
        public GameBoxVector3[][] Samples;

        public Func<float, int, float> OverrideEvaluation
        {
            get; set;
        }

        public float Evaluate(float time, int channel = 0)
        {
            if (OverrideEvaluation != null)
                return OverrideEvaluation(time, channel);
            if (channel < 0 || channel >= Channels)
                return 0;
            int lo = 0, hi = 100;
            for (int i = 0; i < 5; i++)
            {
                int mid = (hi + lo) / 2;
                if (Samples[0][mid].X <= time)
                    lo = mid;
                else
                    hi = mid;
            }
            for (int i = lo; i < hi; i++)
            {
                if (Samples[0][i].X > time || Samples[0][i + 1].X < time)
                    continue;
                GameBoxVector3 a = Samples[channel][i], b = Samples[channel][i + 1];
                return b.X == a.X ? a.Y : a.Y + (b.Y - a.Y) * ((time - a.X) / (b.X - a.X));
            }
            return 0;
        }
    }

    public sealed class ParticleDescription
    {
        public string Name, Texture;
        public float Gravity, NoResistanceSpeed, Resistance;
        public int VelocityResistancePower, SizeResistancePower, TextureSize, AnimationType, Fps;
        public bool RandomFirstFrame, RandomRotationDirection, MirrorX, MirrorY;
        public int RotationStart, RotationEnd, Rows, Columns, BlendMode;
        public ParticleCurve Size, Rotation, Alpha, Color;
    }

    public sealed class ParticleEmitter
    {
        public string Name, ShapePath;
        public GameBoxVector3 Position, Scope, BoxMin, BoxMax;
        public int Shape;
        public float SpeedMultiplier, CycleMin, CycleMax, Hotspot, Falloff;
        public bool Loop;
        public ParticleCurve Direction, RandomVelocity, Emission, Life, Size, Rotation;
    }

    public sealed class ParticleController
    {
        public string Name;
        public int ParticleIndex, EmitterIndex, Capacity;
    }

    public sealed class ParticleFile
    {
        public uint Version;
        public int Capacity, Space, Overflow;
        public float Scale;
        public ParticleDescription[] Particles;
        public ParticleEmitter[] Emitters;
        public ParticleController[] Controllers;
    }

    public sealed class ParticleShapeFile
    {
        public GameBoxVector3[] Points;
    }
}
