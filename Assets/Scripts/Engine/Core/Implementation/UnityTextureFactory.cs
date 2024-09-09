// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Core.Implementation
{
    using System;
    using Abstraction;
    using UnityEngine;

    public sealed class UnityTextureFactory : ITextureFactory
    {
        public ITexture2D CreateTexture(int width, int height, byte[] rgba32DataBuffer)
        {
            if (rgba32DataBuffer.Length < width * height * 4)
            {
                throw new ArgumentException("rgba32DataBuffer.Length < width * height * 4");
            }

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);
            texture.LoadRawTextureData(rgba32DataBuffer);
            texture.Apply(updateMipmaps: false);
            return new UnityTexture2D(texture);
        }

        public ITexture2D CreateWhiteTexture() => new UnityTexture2D(Texture2D.whiteTexture);
    }
}