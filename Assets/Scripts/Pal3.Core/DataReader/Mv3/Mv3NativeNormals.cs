// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Mv3
{
    using System;
    using Primitives;

    /// <summary>The original morph renderer repairs exported normals at each keyframe.</summary>
    public static class Mv3NativeNormals
    {
        private const double LatitudeScale = 81.16901871813289;
        private const double LongitudeScale = 40.584509359066445;
        private const float Pi = 3.1415926535f;

        public static ushort[][] Calculate(Mv3VertFrame[] frames, Mv3Attribute[] attributes)
        {
            var result = new ushort[frames.Length][];
            for (int frame = 0; frame < frames.Length; frame++)
            {
                Mv3Vert[] vertices = frames[frame].Vertices;
                var sums = new GameBoxVector3[vertices.Length];
                foreach (Mv3Attribute attribute in attributes)
                {
                    foreach (Mv3IndexBuffer face in attribute.IndexBuffers)
                    {
                        ushort[] indices = face.TriangleIndex;
                        GameBoxVector3 a = Position(vertices[indices[0]]);
                        GameBoxVector3 b = Position(vertices[indices[1]]) - a;
                        GameBoxVector3 c = Position(vertices[indices[2]]) - a;
                        var cross = new GameBoxVector3(
                            (float)((double)b.Y * c.Z - (double)b.Z * c.Y),
                            (float)((double)b.Z * c.X - (double)b.X * c.Z),
                            (float)((double)b.X * c.Y - (double)b.Y * c.X));
                        GameBoxVector3 normal = Normalize(cross);
                        foreach (ushort index in indices)
                            sums[index] += normal;
                    }
                }
                var packed = new ushort[vertices.Length];
                for (int vertex = 0; vertex < vertices.Length; vertex++)
                    packed[vertex] = Encode(Normalize(sums[vertex]));
                result[frame] = packed;
            }
            return result;
        }

        private static GameBoxVector3 Position(Mv3Vert vertex) => new(vertex.X / 64f, vertex.Y / 64f, vertex.Z / 64f);

        private static GameBoxVector3 Normalize(GameBoxVector3 value)
        {
            double length = Math.Sqrt((double)value.X * value.X + (double)value.Y * value.Y + (double)value.Z * value.Z);
            return length < .005f ? new GameBoxVector3(0, 1, 0) :
                new GameBoxVector3((float)(value.X / length), (float)(value.Y / length), (float)(value.Z / length));
        }

        private static ushort Encode(GameBoxVector3 value)
        {
            if (Math.Abs(value.X) + Math.Abs(value.Y) < 1f / 500f)
                return value.Z > 0 ? (ushort)0 : (ushort)255;
            int longitude = Math.Clamp((int)(Math.Atan2(value.Y, value.X) * LongitudeScale), -127, 127);
            int latitude = Math.Min(255, (int)(Math.Acos(Math.Clamp(value.Z, -1f, 1f)) * LatitudeScale));
            return (ushort)(((longitude << 8) | latitude) & 65535);
        }

        private static readonly Lazy<GameBoxVector3[]> Decoded = new(CreateDecodedTable);

        public static GameBoxVector3 Decode(ushort value) => Decoded.Value[value];

        private static GameBoxVector3[] CreateDecodedTable()
        {
            var values = new GameBoxVector3[65536];
            for (int value = 0; value < values.Length; value++)
                values[value] = DecodeValue((ushort)value);
            return values;
        }

        private static GameBoxVector3 DecodeValue(ushort value)
        {
            float longitude = (sbyte)(value >> 8) * 2 * Pi / 255;
            float latitude = (value & 255) * Pi / 255;
            return new GameBoxVector3((float)(Math.Cos(longitude) * Math.Sin(latitude)),
                (float)(Math.Sin(longitude) * Math.Sin(latitude)), (float)Math.Cos(latitude));
        }
    }
}
