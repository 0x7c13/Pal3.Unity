// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Primitives
{
    [System.Serializable]
    public struct GameBoxMaterial
    {
        public Color Diffuse;
        public Color Ambient;
        public Color Specular;
        public Color Emissive;
        public float SpecularPower;
        public string[] TextureFileNames;
    }
}