// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Rendering
{
    using System;

    using Vector2 = UnityEngine.Vector2;
    using Vector3 = UnityEngine.Vector3;

    public sealed class MeshDataBuffer
    {
        public Vector3[] VertexBuffer;
        public Vector3[] NormalBuffer;
        public Vector2[] UvBuffer;
        public int[] TriangleBuffer;

        public MeshDataBuffer()
        {
        }

        public MeshDataBuffer(
            int vertexBufferSize,
            int normalBufferSize,
            int uvBufferSize,
            int triangleBufferSize)
        {
            AllocateOrResizeBuffers(vertexBufferSize,
                normalBufferSize,
                uvBufferSize,
                triangleBufferSize);
        }

        public void AllocateOrResizeBuffers(
            int vertexBufferSize,
            int normalBufferSize,
            int uvBufferSize,
            int triangleBufferSize)
        {
            if (vertexBufferSize < 0) throw new ArgumentOutOfRangeException(nameof(vertexBufferSize));
            if (normalBufferSize < 0) throw new ArgumentOutOfRangeException(nameof(normalBufferSize));
            if (uvBufferSize < 0) throw new ArgumentOutOfRangeException(nameof(uvBufferSize));
            if (triangleBufferSize < 0) throw new ArgumentOutOfRangeException(nameof(triangleBufferSize));

            VertexBuffer = AllocateOrResize(VertexBuffer, vertexBufferSize);
            NormalBuffer = AllocateOrResize(NormalBuffer, normalBufferSize);
            UvBuffer = AllocateOrResize(UvBuffer, uvBufferSize);
            TriangleBuffer = AllocateOrResize(TriangleBuffer, triangleBufferSize);
        }

        // Keep lighting continuous where UVs split the same original vertex.
        public void RebuildNormalsBySourceVertex(int[] sourceIndices, Vector3[] normals)
        {
            Array.Clear(normals, 0, normals.Length);
            int[] triangles = TriangleBuffer;
            Vector3[] vertices = VertexBuffer;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i];
                int b = triangles[i + 1];
                int c = triangles[i + 2];
                Vector3 normal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
                normals[sourceIndices[a]] += normal;
                normals[sourceIndices[b]] += normal;
                normals[sourceIndices[c]] += normal;
            }

            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = NormalizeFaceNormal(normals[i]);
            }

            // Coincident surfaces can still cancel even after splitting reversed faces.
            // Use the first valid incident face for any referenced vertex with no direction.
            foreach (int index in sourceIndices)
            {
                if (normals[index] == Vector3.zero)
                {
                    RestoreCancelledNormals(sourceIndices, normals);
                    break;
                }
            }

            for (int i = 0; i < sourceIndices.Length; i++)
            {
                NormalBuffer[i] = normals[sourceIndices[i]];
            }
        }

        private void RestoreCancelledNormals(int[] sourceIndices, Vector3[] normals)
        {
            for (int i = 0; i < TriangleBuffer.Length; i += 3)
            {
                int a = TriangleBuffer[i];
                int b = TriangleBuffer[i + 1];
                int c = TriangleBuffer[i + 2];
                int first = sourceIndices[a];
                int second = sourceIndices[b];
                int third = sourceIndices[c];
                bool firstMissing = normals[first] == Vector3.zero;
                bool secondMissing = normals[second] == Vector3.zero;
                bool thirdMissing = normals[third] == Vector3.zero;
                if (!firstMissing && !secondMissing && !thirdMissing) continue;

                Vector3 normal = NormalizeFaceNormal(Vector3.Cross(VertexBuffer[b] - VertexBuffer[a],
                    VertexBuffer[c] - VertexBuffer[a]));
                if (firstMissing) normals[first] = normal;
                if (secondMissing) normals[second] = normal;
                if (thirdMissing) normals[third] = normal;
            }
        }

        private static Vector3 NormalizeFaceNormal(Vector3 normal)
        {
            float length = normal.magnitude;
            return length > 1e-12f ? normal / length : Vector3.zero;
        }

        private static T[] AllocateOrResize<T>(T[] buffer, int size)
        {
            if (buffer == null)
            {
                return new T[size];
            }
            else if (buffer.Length != size)
            {
                Array.Resize(ref buffer, size);
            }
            return buffer;
        }
    }
}