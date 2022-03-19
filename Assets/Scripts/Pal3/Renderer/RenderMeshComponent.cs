// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Renderer
{
    using Core.Renderer;
    using UnityEngine;

    public class RenderMeshComponent
    {
        public Mesh Mesh { get; set; }
        public StaticMeshRenderer MeshRenderer { get; set; }
        public MeshDataBuffer MeshDataBuffer { get; set; }
    }
}