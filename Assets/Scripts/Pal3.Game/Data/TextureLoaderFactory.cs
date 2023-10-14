// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Data
{
    using System;
    using Engine.Core.Abstraction;
    using Engine.DataLoader;

    public sealed class TextureLoaderFactory : ITextureLoaderFactory
    {
        private readonly ITextureLoader _dxtTextureLoader;
        private readonly ITextureLoader _tgaTextureLoader;
        private readonly ITextureLoader _bmpTextureLoader;

        public TextureLoaderFactory(ITextureFactory textureFactory)
        {
            _dxtTextureLoader = new DxtTextureLoader(textureFactory);
            _tgaTextureLoader = new TgaTextureLoader(textureFactory);
            _bmpTextureLoader = new BmpTextureLoader(textureFactory);
        }

        public ITextureLoader GetTextureLoader(string fileExtension)
        {
            if (!fileExtension.StartsWith(".")) fileExtension = "." + fileExtension;
            return fileExtension.ToLower() switch
            {
                ".dds" => _dxtTextureLoader,
                ".tga" => _tgaTextureLoader,
                // NOTE: ".bm" is just a typo found in some of the texture file names
                // used in PAL3 (traditional chinese version), it's actually .bmp.
                ".bmp" or ".bm" => _bmpTextureLoader,
                _ => throw new ArgumentException($"Texture format not supported: {fileExtension}")
            };
        }
    }
}