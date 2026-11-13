// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Engine.DataLoader
{
    using System;
    using System.Buffers;
    using Core.Abstraction;
    using Core.Implementation;
    using Pal3.Core.DataReader.Tga;

    public sealed class TgaTextureLoader : ITextureLoader
    {
        public unsafe ITextureDataSource Load(byte[] data, out bool hasAlphaChannel)
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

            byte[] rgba = ArrayPool<byte>.Shared.Rent(checked(width * height * 4));
            try
            {
                switch (bitDepth)
                {
                    case 24:
                        hasAlphaChannel = false;
                        TgaDecoder.Decode24BitDataToRgba32(data, width, height, rgba);
                        break;
                    case 32:
                        TgaDecoder.Decode32BitDataToRgba32(data, width, height, rgba, out hasAlphaChannel);
                        break;
                    default:
                        throw new Exception("TGA texture had non 32/24 bit depth");
                }
                return Rgba32TextureDataSource.TakePooledBuffer(width, height, rgba, hasTransparency: true);
            }
            catch
            {
                ArrayPool<byte>.Shared.Return(rgba);
                throw;
            }
        }
    }
}
