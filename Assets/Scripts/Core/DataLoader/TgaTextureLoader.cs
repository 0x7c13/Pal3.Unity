// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataLoader
{
    using System;
    using UnityEngine;

    /// <summary>
    /// .tga file loader and Texture2D converter.
    /// </summary>
    public sealed class TgaTextureLoader : ITextureLoader
    {
        private Color32[] _pixels;
        private short _width;
        private short _height;

        public unsafe void Load(byte[] data, out bool hasAlphaChannel)
        {
            byte bitDepth;

            fixed (byte* p = &data[12])
            {
                _width = *(short*)p;
                _height = *(short*)(p + 2);
                bitDepth = *(p + 4);
            }

            var dataStartIndex = 18;
            _pixels = new Color32[_width * _height];

            hasAlphaChannel = false;
            if (bitDepth == 32)
            {
                fixed (byte* srcStart = &data[dataStartIndex], dstStart = &_pixels[0].r)
                {
                    var firstAlpha = *(srcStart + 3);
                    byte* src = srcStart, dst = dstStart;
                    for (var i = 0; i < _width * _height; i++, src+=4, dst += 4)
                    {
                        *dst = *(src + 2);
                        *(dst + 1) = *(src + 1);
                        *(dst + 2) = *src;

                        var alpha = *(src + 3);
                        *(dst + 3) = alpha;

                        if (alpha != firstAlpha) hasAlphaChannel = true;
                    }
                }
            }
            else if (bitDepth == 24)
            {
                fixed (byte* srcStart = &data[dataStartIndex], dstStart = &_pixels[0].r)
                {
                    byte* src = srcStart, dst = dstStart;
                    for (var i = 0; i < _width * _height; i++, src += 3, dst += 4)
                    {
                        *dst = *(src + 2);
                        *(dst + 1) = *(src + 1);
                        *(dst + 2) = *src;
                        *(dst + 3) = 0;
                    }
                }
            }
            else
            {
                throw new Exception("TGA texture had non 32/24 bit depth.");
            }
        }

        public Texture2D ToTexture2D()
        {
            if (_pixels == null) return null;
            var texture = new Texture2D(_width, _height, TextureFormat.RGBA32, mipChain: false);
            texture.SetPixels32(_pixels);
            texture.Apply();
            return texture;
        }
    }
}