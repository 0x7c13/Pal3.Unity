// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Data
{
    using System;
    using Core.DataLoader;

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
                ".bmp" => _bmpTextureLoader ??= new BmpTextureLoader(),
                _ => throw new ArgumentException($"Texture format not supported: {fileExtension}")
            };
        }
    }
}