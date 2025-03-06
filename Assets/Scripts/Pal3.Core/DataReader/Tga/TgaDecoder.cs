// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Tga
{
    using System;

    public static class TgaDecoder
    {
        private const int TGA_FILE_HEADER_SIZE = 18;

        public static byte[] Decode24BitDataToRgba32(byte[] data,
            int width,
            int height)
        {
            byte[] buffer = new byte[width * height * 4];
            Decode24BitDataToRgba32(data, width, height, buffer);
            return buffer;
        }

        public static unsafe void Decode24BitDataToRgba32(byte[] data,
            int width,
            int height,
            byte[] buffer)
        {
            if (buffer == null || buffer.Length < width * height * 4)
            {
                throw new ArgumentException("buffer is null or too small");
            }

            fixed (byte* srcStart = &data[TGA_FILE_HEADER_SIZE], dstStart = buffer)
            {
                byte* src = srcStart, dst = dstStart;
                for (int i = 0; i < width * height; i++, src += 3, dst += 4)
                {
                    *dst = *(src + 2);
                    *(dst + 1) = *(src + 1);
                    *(dst + 2) = *src;
                    *(dst + 3) = 0; // 24-bit don't have alpha
                }
            }
        }

        public static byte[] Decode32BitDataToRgba32(byte[] data,
            int width,
            int height,
            out bool hasAlphaChannel)
        {
            byte[] buffer = new byte[width * height * 4];
            Decode32BitDataToRgba32(data, width, height, buffer, out hasAlphaChannel);
            return buffer;
        }

        public static unsafe void Decode32BitDataToRgba32(byte[] data,
            int width,
            int height,
            byte[] buffer,
            out bool hasAlphaChannel)
        {
            if (buffer == null || buffer.Length < width * height * 4)
            {
                throw new ArgumentException("buffer is null or too small");
            }

            hasAlphaChannel = false;
            fixed (byte* srcStart = &data[TGA_FILE_HEADER_SIZE], dstStart = buffer)
            {
                byte firstAlpha = *(srcStart + 3);
                byte* src = srcStart, dst = dstStart;
                for (var i = 0; i < width * height; i++, src += 4, dst += 4)
                {
                    *dst = *(src + 2);
                    *(dst + 1) = *(src + 1);
                    *(dst + 2) = *src;

                    byte alpha = *(src + 3);
                    *(dst + 3) = alpha;

                    if (alpha != firstAlpha) hasAlphaChannel = true;
                }
            }
        }
    }
}