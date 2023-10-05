// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Data
{
    using Core.FileSystem;
    using Engine.DataLoader;
    using UnityEngine;

    public abstract class TextureProviderBase
    {
        protected Texture2D GetTexture(ICpkFileSystem fileSystem,
            string texturePath,
            ITextureLoader textureLoader,
            out bool hasAlphaChannel)
        {
            // Just to make sure we have the texture
            if (!fileSystem.FileExists(texturePath))
            {
                hasAlphaChannel = false;
                return null;
            }

            var data = fileSystem.ReadAllBytes(texturePath);
            textureLoader.Load(data, out hasAlphaChannel);
            return textureLoader.ToTexture2D();
        }
    }
}