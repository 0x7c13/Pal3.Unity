// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Core.Implementation
{
    using Abstraction;
    using UnityEngine;

    public static class TextureFactory
    {
        public static Texture2D CreateTexture2D(int width, int height, byte[] rawRgbaData)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);
            texture.LoadRawTextureData(rawRgbaData);
            texture.Apply(updateMipmaps: false);
            return texture;
        }
    }
}