// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Rendering
{
    using Vector2 = UnityEngine.Vector2;
    using Vector3 = UnityEngine.Vector3;

    public class MeshDataBuffer
    {
        public Vector3[] VertexBuffer;
        public Vector3[] NormalBuffer;
        public Vector2[] UvBuffer;
    }
}