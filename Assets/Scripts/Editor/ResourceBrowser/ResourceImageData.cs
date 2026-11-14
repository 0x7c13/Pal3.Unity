// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser
{
    using System;
    using System.IO;

    // Validate dimensions before legacy decoders allocate buffers or access unsafe pixel data.
    public static class ResourceImageData
    {
        public static byte[] Prepare(string extension, byte[] data)
        {
            long width, height;
            switch (extension)
            {
                case ".tga":
                    Require(data, 18);
                    if (data[1] != 0 || data[2] != 2 || data[16] is not (24 or 32))
                        throw new InvalidDataException("Only uncompressed true-color 24/32-bit TGA images are supported.");
                    width = Little16(data, 12);
                    height = Little16(data, 14);
                    CheckSize(width, height);
                    int stride = data[16] / 8;
                    int offset = 18 + data[0];
                    Require(data, checked(offset + (int)(width * height * stride)));
                    // The runtime decoder expects a bottom-left origin and no image ID.
                    var normalized = new byte[checked(18 + (int)(width * height * stride))];
                    Array.Copy(data, normalized, 18);
                    normalized[0] = 0;
                    normalized[17] &= 15;
                    for (int y = 0; y < height; y++)
                    {
                        int sourceY = (data[17] & 32) != 0 ? (int)height - y - 1 : y;
                        for (int x = 0; x < width; x++)
                        {
                            int sourceX = (data[17] & 16) != 0 ? (int)width - x - 1 : x;
                            Array.Copy(data, offset + (sourceY * (int)width + sourceX) * stride,
                                normalized, 18 + (y * (int)width + x) * stride, stride);
                        }
                    }
                    return normalized;
                case ".dds":
                    Require(data, 128);
                    if (Little32(data, 0) != 0x20534444)
                        throw new InvalidDataException("Invalid DDS signature.");
                    height = Little32(data, 12);
                    width = Little32(data, 16);
                    break;
                case ".bmp":
                case ".bm":
                    Require(data, 54);
                    if (data[0] != 'B' || data[1] != 'M' || Little32(data, 14) < 40)
                        throw new InvalidDataException("Unsupported BMP header.");
                    width = Math.Abs((long)(int)Little32(data, 18));
                    height = Math.Abs((long)(int)Little32(data, 22));
                    break;
                case ".png":
                    Require(data, 24);
                    if (Big32(data, 0) != 0x89504e47 || Big32(data, 4) != 0x0d0a1a0a || Big32(data, 12) != 0x49484452)
                        throw new InvalidDataException("Invalid PNG header.");
                    width = Big32(data, 16);
                    height = Big32(data, 20);
                    break;
                case ".jpg":
                case ".jpeg":
                    (width, height) = JpegSize(data);
                    break;
                default:
                    throw new InvalidDataException("Unsupported image format.");
            }
            CheckSize(width, height);
            return data;
        }

        private static (long, long) JpegSize(byte[] data)
        {
            Require(data, 4);
            if (data[0] != 255 || data[1] != 216)
                throw new InvalidDataException("Invalid JPEG signature.");
            int offset = 2;
            while (offset < data.Length)
            {
                if (data[offset++] != 255)
                    throw new InvalidDataException("Invalid JPEG marker.");
                while (offset < data.Length && data[offset] == 255)
                    offset++;
                Require(data, offset + 1);
                byte marker = data[offset++];
                if (marker is 0xda or 0xd9)
                    break;
                if (marker is 0x01 or >= 0xd0 and <= 0xd8)
                    continue;
                Require(data, offset + 2);
                int length = data[offset] * 256 + data[offset + 1];
                if (length < 2)
                    throw new InvalidDataException("Invalid JPEG segment length.");
                Require(data, offset + length);
                if (marker >= 0xc0 && marker <= 0xcf && marker is not (0xc4 or 0xc8 or 0xcc))
                {
                    if (length < 8)
                        throw new InvalidDataException("Invalid JPEG frame header.");
                    return (data[offset + 5] * 256 + data[offset + 6], data[offset + 3] * 256 + data[offset + 4]);
                }
                offset += length;
            }
            throw new InvalidDataException("JPEG dimensions were not found.");
        }

        private static void CheckSize(long width, long height)
        {
            if (width <= 0 || height <= 0 || width > 16384 || height > 16384 || width * height > 32 * 1024 * 1024)
                throw new InvalidDataException("Image dimensions are invalid or exceed the preview limit (16384 per side, 32 megapixels).");
        }

        private static void Require(byte[] data, int length)
        {
            if (data.Length < length)
                throw new InvalidDataException("Truncated image data.");
        }

        private static int Little16(byte[] data, int at) => data[at] | data[at + 1] << 8;

        private static uint Little32(byte[] data, int at) => (uint)(data[at] | data[at + 1] << 8 | data[at + 2] << 16 | data[at + 3] << 24);

        private static uint Big32(byte[] data, int at) => (uint)(data[at] << 24 | data[at + 1] << 16 | data[at + 2] << 8 | data[at + 3]);
    }
}
