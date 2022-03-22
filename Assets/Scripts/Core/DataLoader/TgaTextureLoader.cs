#define TGA_TEXTURE_LOADER_USE_UNSAFE

namespace Core.DataLoader
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using UnityEngine;

    public class TgaTextureLoader : ITextureLoader
    {
        #if TGA_TEXTURE_LOADER_USE_UNSAFE
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
        #else
        public Texture2D LoadTexture(byte[] data, out bool hasAlphaChannel)
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);

            // Skip header
            reader.BaseStream.Seek(12, SeekOrigin.Begin);

            var width = reader.ReadInt16();
            var height = reader.ReadInt16();
            var bitDepth = reader.ReadByte();

            // Skip a byte of header information we don't care about.
            reader.BaseStream.Seek(1, SeekOrigin.Current);

            var colors = new Color32[width * height];

            hasAlphaChannel = false;

            if (bitDepth == 32)
            {
                var blue = reader.ReadByte();
                var green = reader.ReadByte();
                var red = reader.ReadByte();
                var alpha = reader.ReadByte();
                colors[0].r = red;
                colors[0].g = green;
                colors[0].b = blue;
                colors[0].a = alpha;
                var firstAlpha = alpha;

                for (var i = 1; i < width * height; i++)
                {
                    blue = reader.ReadByte();
                    green = reader.ReadByte();
                    red = reader.ReadByte();
                    alpha = reader.ReadByte();
                    colors[i].r = red;
                    colors[i].g = green;
                    colors[i].b = blue;
                    colors[i].a = alpha;
                    if (alpha != firstAlpha) hasAlphaChannel = true;
                }
            }
            else if (bitDepth == 24)
            {
                for (var i = 0; i < width * height; i++)
                {
                    var blue = reader.ReadByte();
                    var green = reader.ReadByte();
                    var red = reader.ReadByte();
                    colors[i].r = red;
                    colors[i].g = green;
                    colors[i].b = blue;
                    colors[i].a = 0;
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
        #endif
    }
}