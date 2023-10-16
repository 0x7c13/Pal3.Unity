// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Data
{
    using System;
    using Core.FileSystem;
    using Engine.Core.Abstraction;

    public abstract class TextureProviderBase
    {
        protected ITexture2D GetTexture(ICpkFileSystem fileSystem,
            string texturePath,
            ITextureLoader textureLoader,
            out bool hasAlphaChannel)
        {
            byte[] data;

            try
            {
                data = fileSystem.ReadAllBytes(texturePath);
            }
            catch (Exception)
            {
                hasAlphaChannel = false;
                return null;
            }

            textureLoader.Load(data, out hasAlphaChannel);
            return textureLoader.ToTexture();
        }
    }
}