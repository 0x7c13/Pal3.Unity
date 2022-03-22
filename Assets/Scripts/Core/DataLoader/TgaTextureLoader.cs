// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataLoader
{
    using System;
    using UnityEngine;

    public class TgaTextureLoader : ITextureLoader
    {
        public unsafe Texture2D LoadTexture(byte[] data, out bool hasAlphaChannel)
        {
            short width;
            short height;
            byte bitDepth;

            fixed (byte* p = &data[12])
            {
                width = *(short*)p;
                height = *(short*)(p + 2);
                bitDepth = *(p + 4);
            }

            var dataStartIndex = 18;
            var colors = new Color32[width * height];

            hasAlphaChannel = false;
            if (bitDepth == 32)
            {
                fixed (byte* srcStart = &data[dataStartIndex], dstStart = &colors[0].r)
                {
                    var firstAlpha = *(srcStart + 3);
                    byte* src = srcStart, dst = dstStart;
                    for (var i = 0; i < width * height; i++, src+=4, dst += 4)
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
                fixed (byte* srcStart = &data[dataStartIndex], dstStart = &colors[0].r)
                {
                    byte* src = srcStart, dst = dstStart;
                    for (var i = 0; i < width * height; i++, src += 3, dst += 4)
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

            var texture = new Texture2D(width, height);
            texture.SetPixels32(colors);
            texture.Apply();
            return texture;
        }
    }
}