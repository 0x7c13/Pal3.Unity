// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Par
{
    using System;
    using System.IO;

    // PAL3A.exe loader 0x4c65a3. This is independent of the sgpar format.
    public sealed class SparFileReader : IFileReader<SparFile>
    {
        public SparFile Read(IBinaryReader r, int codepage)
        {
            byte[] header = r.ReadExactly(264);
            using IBinaryReader h = new SafeBinaryReader(header);
            float version = h.ReadSingle();
            int count = Count(h, 128);
            if (version != 1 || h.ReadString(40) != "__PARTICLE_EFFECT(Softstar_Ltd)_EXPORT__")
                throw new InvalidDataException("Unsupported SPar export header");
            var file = new SparFile { Version = version, Groups = new SparGroup[count] };
            for (int i = 0; i < count; i++)
            {
                using IBinaryReader g = new SafeBinaryReader(r.ReadExactly(544));
                g.ReadUInt32(); // native editor/runtime pointer, not a file offset
                var group = new SparGroup { Capacity = Count(g, 65536), VertexCapacity = Count(g, 393216), Mode = g.ReadInt32(), Loop = g.ReadInt32() != 0 };
                int frames = Count(g, 4096), keys = Count(g, 4096);
                group.Name = g.ReadString(256, codepage);
                group.Texture = g.ReadString(260, codepage);
                group.TextureFrames = new float[frames][];
                for (int frame = 0; frame < frames; frame++)
                    group.TextureFrames[frame] = Floats(r, 4);
                group.Keyframes = new SparKeyframe[keys];
                for (int key = 0; key < keys; key++)
                    group.Keyframes[key] = Keyframe(r);
                file.Groups[i] = group;
            }
            Require(r, 20);
            file.Shader = r.ReadInt32();
            file.ShaderParameters = Floats(r, 4);
            file.UnusedByteCount = checked((int)(r.Length - r.Position));
            return file;
        }

        private static SparKeyframe Keyframe(IBinaryReader r)
        {
            Require(r, 28);
            var key = new SparKeyframe
            {
                Time = r.ReadSingle(),
                Position = r.ReadGameBoxVector3(),
                EmissionMinimum = r.ReadInt32(),
                EmissionMaximum = r.ReadInt32(),
                Shape = r.ReadInt32()
            };
            int values = key.Shape switch
            {
                0 => 0,
                1 => 1,
                2 => 6,
                3 => checked(Count(r, 1000000) * 3),
                _ => throw new InvalidDataException($"Unknown SPar spawn shape {key.Shape}")
            };
            key.ShapeParameters = Floats(r, values);
            Require(r, 4);
            key.HasPath = r.ReadInt32() != 0;
            if (key.HasPath)
            {
                Require(r, 4);
                key.PathRate = r.ReadSingle();
                int count = Count(r, 1000000);
                Require(r, checked(count * 12));
                key.PathPoints = new Primitives.GameBoxVector3[count];
                for (int i = 0; i < count; i++)
                    key.PathPoints[i] = r.ReadGameBoxVector3();
            }
            else
                key.PathPoints = Array.Empty<Primitives.GameBoxVector3>();
            Require(r, 96);
            key.VelocityMinimum = r.ReadGameBoxVector3();
            key.VelocityMaximum = r.ReadGameBoxVector3();
            key.RotationMinimum = r.ReadSingle();
            key.RotationMaximum = r.ReadSingle();
            key.SpinMinimum = r.ReadSingle();
            key.SpinMaximum = r.ReadSingle();
            key.ColorMinimum = r.ReadGameBoxVector3();
            key.ColorMaximum = r.ReadGameBoxVector3();
            key.AlphaMinimum = r.ReadSingle();
            key.AlphaMaximum = r.ReadSingle();
            key.LifeMinimum = r.ReadSingle();
            key.LifeMaximum = r.ReadSingle();
            key.SizeMinimum = r.ReadSingle();
            key.SizeMaximum = r.ReadSingle();
            key.FrameMinimum = r.ReadInt32();
            key.FrameMaximum = r.ReadInt32();
            int[][] parameters = { new[] { 0, 0, 3, 4, 0, 0, 0 }, new[] { 0, 0 }, new[] { 0, 0, 6 }, new[] { 0, 0, 0 }, new[] { 0, 1, 1 }, new[] { 0, 4 } };
            key.Controllers = new SparController[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                Require(r, 4);
                int kind = r.ReadInt32();
                if ((uint)kind >= (uint)parameters[i].Length)
                    throw new InvalidDataException($"Unknown SPar controller {i}:{kind}");
                key.Controllers[i] = new SparController { Kind = kind, Parameters = Floats(r, parameters[i][kind]) };
            }
            key.Reserved = Floats(r, 4);
            return key;
        }

        private static int Count(IBinaryReader r, int maximum)
        {
            Require(r, 4);
            int count = r.ReadInt32();
            if (count < 0 || count > maximum)
                throw new InvalidDataException($"Invalid SPar count {count}");
            return count;
        }

        private static float[] Floats(IBinaryReader r, int count)
        {
            Require(r, checked(count * 4));
            return r.ReadSingles(count);
        }

        private static void Require(IBinaryReader r, int count)
        {
            if (count > r.Length - r.Position)
                throw new EndOfStreamException("Truncated SPar file");
        }
    }
}
