// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Data
{
    using System;
    using Engine.DataLoader;

    public class TextureLoaderFactory : ITextureLoaderFactory
    {
        private ITextureLoader _dxtTextureLoader;
        private ITextureLoader _tgaTextureLoader;
        private ITextureLoader _bmpTextureLoader;

        public ITextureLoader GetTextureLoader(string fileExtension)
        {
            if (!fileExtension.StartsWith(".")) fileExtension = "." + fileExtension;
            return fileExtension.ToLower() switch
            {
                ".dds" => _dxtTextureLoader ??= new DxtTextureLoader(),
                ".tga" => _tgaTextureLoader ??= new TgaTextureLoader(),
                // NOTE: ".bm" is just a typo found in some of the texture file names
                // used in PAL3 (traditional chinese version), it's actually .bmp.
                ".bmp" or ".bm" => _bmpTextureLoader ??= new BmpTextureLoader(),
                _ => throw new ArgumentException($"Texture format not supported: {fileExtension}")
            };
        }
    }
}