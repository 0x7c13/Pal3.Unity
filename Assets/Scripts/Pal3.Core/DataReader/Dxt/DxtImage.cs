// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Dxt
{
    using System;
    using System.IO;

    /// <summary>A validated, two-dimensional DXT1/DXT3 DDS and its mip layout.</summary>
    public sealed class DxtImage
    {
        public byte[] Data { get; }
        public int Width { get; }
        public int Height { get; }
        public int MipCount { get; }
        public bool HasAlphaChannel { get; }
        public bool HasTransparency { get; }
        private readonly int[] _offsets;

        public DxtImage(byte[] data)
        {
            if (data == null || data.Length < 128) throw new InvalidDataException("Truncated DDS header");
            using var reader = new BinaryReader(new MemoryStream(data));
            if (reader.ReadUInt32() != 0x20534444) throw new InvalidDataException("Invalid DDS magic");
            DxtHeader header = DxtHeader.ReadHeader(reader);
            if (header.Size != 124 || header.DxtPixelFormat.Size != 32 ||
                (header.DxtPixelFormat.Flags & 4) == 0 || header.Caps2 != 0 || header.Depth > 1 ||
                header.Width <= 0 || header.Height <= 0)
                throw new InvalidDataException("Unsupported DDS header or dimensions");
            if (header.DxtPixelFormat.Format is not ("DXT1" or "DXT3"))
                throw new InvalidDataException($"Unsupported DDS format: {header.DxtPixelFormat.Format}");
            Width = header.Width;
            Height = header.Height;
            HasAlphaChannel = header.DxtPixelFormat.Format == "DXT3";
            MipCount = Math.Max(1, header.MipMapCount);
            int maximumMips = 1;
            for (int size = Math.Max(Width, Height); size > 1; size >>= 1) maximumMips++;
            if (MipCount > maximumMips || header.MipMapCount < 0)
                throw new InvalidDataException("Invalid DDS mip count");
            // Also bound the decoded allocation before entering the unsafe block decoders.
            if ((long)Width * Height * 4 > int.MaxValue)
                throw new InvalidDataException("DDS dimensions are too large");
            _offsets = new int[MipCount];
            long offset = 128;
            for (int mip = 0; mip < MipCount; mip++)
            {
                _offsets[mip] = checked((int)offset);
                offset += ((GetWidth(mip) + 3L) / 4) * ((GetHeight(mip) + 3L) / 4) * (HasAlphaChannel ? 16 : 8);
                if (offset > data.Length) throw new InvalidDataException("Truncated DDS mip data");
            }
            Data = data;
            // DXT1 also has a one-bit transparent palette entry. Preserve its coverage even
            // though the loader's legacy hasAlphaChannel flag only identifies DXT3 textures.
            HasTransparency = HasAlphaChannel || HasTransparentDxt1Block();
        }

        private bool HasTransparentDxt1Block()
        {
            int blocks = checked(((Width + 3) / 4) * ((Height + 3) / 4));
            for (int block = 0; block < blocks; block++)
            {
                int offset = 128 + block * 8;
                if (BitConverter.ToUInt16(Data, offset) > BitConverter.ToUInt16(Data, offset + 2)) continue;
                uint indices = BitConverter.ToUInt32(Data, offset + 4);
                if ((indices & (indices >> 1) & 0x55555555u) != 0) return true;
            }
            return false;
        }

        public int GetWidth(int mip) => Math.Max(1, Width >> mip);
        public int GetHeight(int mip) => Math.Max(1, Height >> mip);
        public int GetOffset(int mip) => _offsets[mip];

        public void Decode(int mip, byte[] destination)
        {
            if (HasAlphaChannel) Dxt3Decoder.ToRgba32(Data, GetWidth(mip), GetHeight(mip), destination, _offsets[mip]);
            else Dxt1Decoder.ToRgba32(Data, GetWidth(mip), GetHeight(mip), destination, _offsets[mip]);
        }

        /// <summary>Read a base-level pixel without keeping an uncompressed CPU texture copy.</summary>
        public (byte r, byte g, byte b, byte a) GetPixel(int x, int y)
        {
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
                throw new ArgumentOutOfRangeException(nameof(x), "DDS pixel is outside the base level");
            int block = (y / 4) * ((Width + 3) / 4) + x / 4;
            var pixels = new byte[64];
            int offset = 128 + block * (HasAlphaChannel ? 16 : 8);
            if (HasAlphaChannel) Dxt3Decoder.ToRgba32(Data, 4, 4, pixels, offset);
            else Dxt1Decoder.ToRgba32(Data, 4, 4, pixels, offset);
            int index = ((y % 4) * 4 + x % 4) * 4;
            return (pixels[index], pixels[index + 1], pixels[index + 2], pixels[index + 3]);
        }
    }
}
