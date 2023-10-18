// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Core.Implementation
{
    using System;
    using Abstraction;
    using UnityEngine;

    public sealed class UnityTextureFactory : ITextureFactory
    {
        public ITexture2D CreateTexture(int width, int height, byte[] rgbaData)
        {
            if (rgbaData.Length < width * height * 4)
            {
                throw new ArgumentException("rgbaData.Length < width * height * 4");
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);
            texture.LoadRawTextureData(rgbaData);
            texture.Apply(updateMipmaps: false);
            return new UnityTexture2D(texture);
        }

        public ITexture2D CreateWhiteTexture() => new UnityTexture2D(Texture2D.whiteTexture);
    }
}