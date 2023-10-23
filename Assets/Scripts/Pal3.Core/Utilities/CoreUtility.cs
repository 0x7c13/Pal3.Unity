// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Utilities
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Primitives;

    public static class CoreUtility
    {
        public static T ReadStruct<T>(Stream stream) where T : struct
        {
            int structSize = Marshal.SizeOf(typeof(T));
            byte[] buffer = new byte[structSize];
            _ = stream.Read(buffer);
            return ReadStructInternal<T>(buffer);
        }

        public static T ReadStruct<T>(Span<byte> bytes, int offset = 0) where T : struct
        {
            return ReadStructInternal<T>(bytes, offset);
        }

        private static unsafe T ReadStructInternal<T>(Span<byte> bytes, int offset = 0) where T : struct
        {
            fixed (byte* ptr = &bytes[offset])
            {
                return (T) (Marshal.PtrToStructure((IntPtr) ptr, typeof(T)) ?? default(T));
            }
        }

        public static int GetFloorIndex<T>(T[] valuesInIncreasingOrder, T lookUpValue)
        {
            /*
             * The index of the specified value in the specified array, if value is found;
             * otherwise, a negative number. If value is not found and value is less than
             * one or more elements in array, the negative number returned is the bitwise
             * complement of the index of the first element that is larger than value.
             * If value is not found and value is greater than all elements in array,
             * the negative number returned is the bitwise complement of
             * (the index of the last element plus 1).
             */
            int index = Array.BinarySearch(valuesInIncreasingOrder, lookUpValue);

            if (index < 0)
            {
                return -index - 2;
            }

            return index;
        }

        public static GameBoxVector3[] CalculateNormals(GameBoxVector3[] vertices, int[] triangles)
        {
            GameBoxVector3[] normals = new GameBoxVector3[vertices.Length];

            for (var face = 0; face < triangles.Length / 3; face++)
            {
                int v1 = triangles[face * 3];
                int v2 = triangles[face * 3 + 1];
                int v3 = triangles[face * 3 + 2];

                GameBoxVector3 pt1 = vertices[v1];
                GameBoxVector3 pt2 = vertices[v2];
                GameBoxVector3 pt3 = vertices[v3];

                GameBoxVector3 d1 = pt2 - pt1;
                GameBoxVector3 d2 = pt3 - pt1;
                GameBoxVector3 normal = GameBoxVector3.Normalize(GameBoxVector3.Cross(d1, d2));

                normals[v1] += normal;
                normals[v2] += normal;
                normals[v3] += normal;
            }

            for (var i = 0; i < normals.Length; i++)
            {
                if (normals[i] == GameBoxVector3.Zero)
                {
                    normals[i] = new GameBoxVector3(0f, 1f, 0f); // Up
                }
                else
                {
                    normals[i] = GameBoxVector3.Normalize(normals[i]);
                }
            }

            return normals;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Rgb565ToRgb888(ushort rgb565Color, out byte r, out byte g, out byte b)
        {
            var temp = (rgb565Color >> 11) * 255 + 16;
            r = (byte) ((temp / 32 + temp) / 32);
            temp = ((rgb565Color & 0x07E0) >> 5) * 255 + 32;
            g = (byte) ((temp / 64 + temp) / 64);
            temp = (rgb565Color & 0x001F) * 255 + 16;
            b = (byte) ((temp / 32 + temp) / 32);
        }

        public static string GetDirectoryName(string filePath, char directorySeparatorChar)
        {
            return !filePath.Contains(directorySeparatorChar) ?
                string.Empty :
                filePath[..filePath.LastIndexOf(directorySeparatorChar)];
        }

        public static string GetFileName(string filePath, char directorySeparatorChar)
        {
            return !filePath.Contains(directorySeparatorChar) ?
                string.Empty :
                filePath[(filePath.LastIndexOf(directorySeparatorChar)+1)..];
        }

        public static bool IsVersionGreater(string latestVersion, string currentVersion)
        {
            string[] latestParts = latestVersion.Split('.');
            string[] currentParts = currentVersion.Split('.');

            for (var i = 0; i < Math.Min(latestParts.Length, currentParts.Length); i++)
            {
                int latest = int.Parse(latestParts[i]);
                int current = int.Parse(currentParts[i]);
                if (latest == current) continue;
                return latest > current;
            }

            return latestParts.Length > currentParts.Length;
        }
    }
}