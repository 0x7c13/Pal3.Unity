// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Utilities
{
    using System;
    using System.Collections.Generic;
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

        public static (int[] indices, int count) GetNormalIndices(int[] corners, int sourceVertexCount,
            int[] vertexRepresentatives = null)
        {
            // Match faces by deformation identity while keeping normals indexed by source vertex.
            int[] faceCorners = corners;
            if (vertexRepresentatives != null)
            {
                faceCorners = new int[corners.Length];
                for (int i = 0; i + 2 < corners.Length; i += 3)
                {
                    int a = vertexRepresentatives[corners[i]];
                    int b = vertexRepresentatives[corners[i + 1]];
                    int c = vertexRepresentatives[corners[i + 2]];
                    // Preserve exact-index matching for collapsed faces.
                    bool collapsed = a == b || b == c || a == c;
                    faceCorners[i] = collapsed ? corners[i] : a;
                    faceCorners[i + 1] = collapsed ? corners[i + 1] : b;
                    faceCorners[i + 2] = collapsed ? corners[i + 2] : c;
                }
            }

            int faceCount = corners.Length / 3;
            var firstFaces = new Dictionary<(int, int, int), int>();
            var matchingFaces = new int[faceCount];
            bool[] pairedFaces = null;
            for (int face = 0; face < faceCount; face++)
            {
                int a = faceCorners[face * 3];
                int b = faceCorners[face * 3 + 1];
                int c = faceCorners[face * 3 + 2];
                if (a == b || b == c || a == c) continue;

                // Rotate to the smallest corner without changing the winding.
                if (b < a && b < c) (a, b, c) = (b, c, a);
                else if (c < a && c < b) (a, b, c) = (c, a, b);
                if (firstFaces.TryGetValue((a, c, b), out int opposite))
                {
                    pairedFaces ??= new bool[faceCount];
                    pairedFaces[opposite] = true;
                    matchingFaces[face] = -(opposite + 1);
                }
                else if (firstFaces.TryGetValue((a, b, c), out int same))
                {
                    matchingFaces[face] = same + 1;
                }
                else
                {
                    firstFaces.Add((a, b, c), face);
                }
            }

            if (pairedFaces == null) return (corners, sourceVertexCount);

            sbyte[] sides = OrientPairedFaces(faceCorners, firstFaces.Values, pairedFaces);
            var backFaces = new bool[faceCount];
            var frontVertices = new bool[sourceVertexCount];
            for (int face = 0; face < faceCount; face++)
            {
                int match = matchingFaces[face];
                int first = match == 0 ? face : Math.Abs(match) - 1;
                if (pairedFaces[first] && (sides[first] < 0) != (match < 0))
                {
                    backFaces[face] = true;
                    continue;
                }

                int corner = face * 3;
                frontVertices[corners[corner]] = true;
                frontVertices[corners[corner + 1]] = true;
                frontVertices[corners[corner + 2]] = true;
            }

            // A source vertex that already belongs to only one side keeps its normal group.
            int[] normalIndices = null;
            for (int face = 0; face < faceCount; face++)
            {
                if (!backFaces[face]) continue;
                int corner = face * 3;
                for (int i = corner; i < corner + 3; i++)
                {
                    if (!frontVertices[corners[i]]) continue;
                    normalIndices ??= (int[])corners.Clone();
                    normalIndices[i] += sourceVertexCount;
                }
            }

            return normalIndices == null ? (corners, sourceVertexCount) : (normalIndices, sourceVertexCount * 2);
        }

        private static sbyte[] OrientPairedFaces(int[] corners, IEnumerable<int> firstFaces, bool[] pairedFaces)
        {
            var edges = new Dictionary<(int, int), (int first, int second, bool firstAscending, bool secondAscending)>();
            foreach (int face in firstFaces)
            {
                for (int edge = 0; edge < 3; edge++)
                {
                    var (key, ascending) = GetEdge(face, edge);
                    if (!edges.TryGetValue(key, out var adjacent))
                        edges.Add(key, (face, -1, ascending, false));
                    else if (adjacent.second == -1)
                        edges[key] = (adjacent.first, face, adjacent.firstAscending, ascending);
                    else
                        edges[key] = (adjacent.first, -2, false, false);
                }
            }

            var sides = new sbyte[pairedFaces.Length];
            var pending = new Stack<int>();
            // Prefer the orientation of any adjoining single-sided surface.
            foreach (int face in firstFaces)
            {
                if (!pairedFaces[face]) Walk(face);
            }
            foreach (int face in firstFaces)
            {
                if (sides[face] == 0) Walk(face);
            }

            return sides;

            void Walk(int seed)
            {
                sides[seed] = 1;
                pending.Push(seed);
                while (pending.TryPop(out int face))
                {
                    for (int edge = 0; edge < 3; edge++)
                    {
                        var (key, _) = GetEdge(face, edge);
                        var (first, second, firstAscending, secondAscending) = edges[key];
                        if (second < 0) continue; // Border or non-manifold edge.
                        int neighbor = face == first ? second : first;
                        if (!pairedFaces[neighbor] || sides[neighbor] != 0) continue;

                        // Adjacent faces on the same side traverse a shared edge in opposite directions.
                        sides[neighbor] = firstAscending == secondAscending ? (sbyte)-sides[face] : sides[face];
                        pending.Push(neighbor);
                    }
                }
            }

            ((int, int) key, bool ascending) GetEdge(int face, int edge)
            {
                int from = corners[face * 3 + edge];
                int to = corners[face * 3 + (edge + 1) % 3];
                return from < to ? ((from, to), true) : ((to, from), false);
            }
        }

        public static GameBoxVector3[] CalculateNormals(GameBoxVector3[] vertices, int[] triangles)
        {
            GameBoxVector3[] normals = new GameBoxVector3[vertices.Length];

            for (int face = 0; face < triangles.Length / 3; face++)
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

            for (int i = 0; i < normals.Length; i++)
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
            int temp = (rgb565Color >> 11) * 255 + 16;
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

            for (int i = 0; i < Math.Min(latestParts.Length, currentParts.Length); i++)
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