// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

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