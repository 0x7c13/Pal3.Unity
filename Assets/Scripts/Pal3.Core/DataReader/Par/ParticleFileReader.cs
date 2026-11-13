// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Par
{
    using System;
    using System.IO;

    public sealed class ParticleFileReader : IFileReader<ParticleFile>
    {
        public ParticleFile Read(IBinaryReader r, int codepage)
        {
            if (r.ReadString(6) != "sgpar\0")
                throw new InvalidDataException("Not an sgpar particle file (SPar is not supported)");
            uint version = r.ReadUInt32();
            if (version != 0x10000 && version != 0x10001)
                throw new InvalidDataException($"Unsupported sgpar version {version:X}");
            var file = new ParticleFile
            {
                Version = version,
                Capacity = Count(r, 65536),
                Space = r.ReadInt32(),
                Overflow = r.ReadInt32(),
                Scale = version > 0x10000 ? r.ReadSingle() : 1f
            };
            if (file.Space < 0 || file.Space > 1 || file.Overflow < 0 || file.Overflow > 2 || !Finite(file.Scale) || file.Scale <= 0)
                throw new InvalidDataException("Invalid sgpar scene parameters");
            int np = Count(r, 64), ne = Count(r, 64), nc = Count(r, 64);
            file.Particles = new ParticleDescription[np];
            file.Emitters = new ParticleEmitter[ne];
            file.Controllers = new ParticleController[nc];
            for (int i = 0; i < np; i++)
                file.Particles[i] = ReadParticle(r, codepage);
            for (int i = 0; i < ne; i++)
                file.Emitters[i] = ReadEmitter(r, codepage, version);
            for (int i = 0; i < nc; i++)
            {
                var c = new ParticleController
                {
                    Name = r.ReadString(64, codepage),
                    ParticleIndex = r.ReadInt32(),
                    EmitterIndex = r.ReadInt32(),
                    Capacity = Count(r, 65536)
                };
                if (c.ParticleIndex < 0 || c.ParticleIndex >= np || c.EmitterIndex < 0 || c.EmitterIndex >= ne)
                    throw new InvalidDataException("Invalid sgpar controller reference");
                file.Controllers[i] = c;
            }
            if (r.Position != r.Length)
                throw new InvalidDataException("Unexpected sgpar trailing data");
            return file;
        }

        private static ParticleDescription ReadParticle(IBinaryReader r, int cp)
        {
            var p = new ParticleDescription { Name = r.ReadString(32, cp) };
            r.ReadInt32(); // native BOOL lighting (unused by original renderer)
            p.Gravity = r.ReadSingle();
            p.NoResistanceSpeed = r.ReadSingle();
            p.VelocityResistancePower = r.ReadInt32();
            p.SizeResistancePower = r.ReadInt32();
            p.Resistance = r.ReadSingle();
            p.Texture = r.ReadString(256, cp);
            p.TextureSize = r.ReadInt32();
            p.AnimationType = r.ReadInt32();
            p.Fps = r.ReadInt32();
            p.RandomFirstFrame = r.ReadInt32() != 0;
            p.RotationStart = r.ReadInt32();
            p.RotationEnd = r.ReadInt32();
            p.RandomRotationDirection = r.ReadInt32() != 0;
            p.MirrorX = r.ReadInt32() != 0;
            p.MirrorY = r.ReadInt32() != 0;
            p.Rows = r.ReadInt32();
            p.Columns = r.ReadInt32();
            p.BlendMode = r.ReadInt32();
            if (p.Rows < 1 || p.Columns < 1 || p.Rows > 256 || p.Columns > 256)
                throw new InvalidDataException("Invalid particle atlas");
            p.Size = ReadCurve(r, cp);
            p.Rotation = ReadCurve(r, cp);
            p.Alpha = ReadCurve(r, cp);
            p.Color = ReadCurve(r, cp);
            return p;
        }

        private static ParticleEmitter ReadEmitter(IBinaryReader r, int cp, uint version)
        {
            var e = new ParticleEmitter { Name = r.ReadString(64, cp), Position = r.ReadGameBoxVector3(), Scope = r.ReadGameBoxVector3(), Shape = r.ReadInt32() };
            e.ShapePath = version > 0x10000 ? r.ReadString(64, cp) : "";
            e.SpeedMultiplier = version > 0x10000 ? r.ReadSingle() : 1f;
            e.CycleMin = r.ReadSingle();
            e.CycleMax = r.ReadSingle();
            e.Loop = r.ReadInt32() != 0;
            r.ReadInt32(); // lighton
            e.Hotspot = r.ReadSingle();
            e.Falloff = r.ReadSingle();
            e.BoxMin = r.ReadGameBoxVector3();
            e.BoxMax = r.ReadGameBoxVector3();
            if (e.Shape < 0 || e.Shape > 2)
                throw new InvalidDataException("Invalid sgpar emitter shape");
            e.Direction = ReadCurve(r, cp);
            e.RandomVelocity = ReadCurve(r, cp);
            e.Emission = ReadCurve(r, cp);
            e.Life = ReadCurve(r, cp);
            e.Size = ReadCurve(r, cp);
            e.Rotation = ReadCurve(r, cp);
            return e;
        }

        private static ParticleCurve ReadCurve(IBinaryReader r, int cp)
        {
            int channels = Count(r, 3), points = Count(r, 20);
            var curve = new ParticleCurve { Channels = channels, Name = r.ReadString(64, cp), Samples = new Primitives.GameBoxVector3[3][] };
            r.ReadString(128, cp);
            r.ReadGameBoxVector3();
            r.ReadGameBoxVector3();
            r.Seek(2 * 3 * 20 * 12, SeekOrigin.Current); // editor and real control points; runtime uses stored samples
            for (int c = 0; c < 3; c++)
                curve.Samples[c] = r.ReadGameBoxVector3s(101);
            for (int c = 0; c < channels; c++)
                for (int i = 0; i < 101; i++)
                {
                    var p = curve.Samples[c][i];
                    // Shipped curves can double back in x. Preserve their original
                    // sample order and lookup semantics instead of sorting them.
                    if (!Finite(p.X) || !Finite(p.Y))
                        throw new InvalidDataException($"Invalid particle curve {curve.Name} channel={c} sample={i} x={p.X} y={p.Y} previous={(i > 0 ? curve.Samples[c][i - 1].X : 0)}");
                }
            return curve;
        }

        private static int Count(IBinaryReader r, int max)
        {
            int value = r.ReadInt32();
            if (value < 0 || value > max)
                throw new InvalidDataException("Invalid sgpar count");
            return value;
        }

        private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }

    public sealed class ParticleShapeFileReader : IFileReader<ParticleShapeFile>
    {
        public ParticleShapeFile Read(IBinaryReader r, int codepage)
        {
            if (r.ReadString(4) != "csf\0")
                throw new InvalidDataException("Invalid CSF header");
            r.ReadUInt32();
            int count = r.ReadInt32();
            if (count < 2 || count > 1000000 || count > (r.Length - r.Position) / 12)
                throw new InvalidDataException("Invalid CSF point count");
            var result = new ParticleShapeFile { Points = r.ReadGameBoxVector3s(count) };
            if (r.Position != r.Length)
                throw new InvalidDataException("Unexpected CSF trailing data");
            return result;
        }
    }
}
