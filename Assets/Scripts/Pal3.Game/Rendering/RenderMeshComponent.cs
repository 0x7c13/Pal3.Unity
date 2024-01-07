// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Rendering
{
    using Engine.Renderer;
    using UnityEngine;

    public sealed class RenderMeshComponent
    {
        public Mesh Mesh { get; set; }
        public StaticMeshRenderer MeshRenderer { get; set; }
        public MeshDataBuffer MeshDataBuffer { get; set; }
    }
}