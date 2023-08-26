// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Dxt
{
    using System.IO;

    public sealed class DxtPixelFormat
    {
        public int Size;
        public int Flags;
        public string Format;
        public int RGBBitCount;
        public int RBitMask;
        public int GBitMask;
        public int BBitMask;
        public int ABitMask;

        public static DxtPixelFormat ReadFormat(BinaryReader reader)
        {
            return new DxtPixelFormat
            {
                Size = reader.ReadInt32(),
                Flags = reader.ReadInt32(),
                Format = new string(reader.ReadChars(4)),
                RGBBitCount = reader.ReadInt32(),
                RBitMask = reader.ReadInt32(),
                GBitMask = reader.ReadInt32(),
                BBitMask = reader.ReadInt32(),
                ABitMask = reader.ReadInt32()
            };
        }
    }

    public sealed class DxtHeader
    {
        public int Size;
        public int Flags;
        public int Height;
        public int Width;
        public int PitchOrLinearSize;
        public int Depth;
        public int MipMapCount;
        public byte[] ReservedBytes; // 44 bytes
        public DxtPixelFormat DxtPixelFormat;
        public int Caps1;
        public int Caps2;
        public int Caps3;
        public int Caps4;
        public int Reserved;

        public static DxtHeader ReadHeader(BinaryReader reader)
        {
            return new DxtHeader
            {
                Size = reader.ReadInt32(),
                Flags = reader.ReadInt32(),
                Height = reader.ReadInt32(),
                Width = reader.ReadInt32(),
                PitchOrLinearSize = reader.ReadInt32(),
                Depth = reader.ReadInt32(),
                MipMapCount = reader.ReadInt32(),
                ReservedBytes = reader.ReadBytes(44),
                DxtPixelFormat = DxtPixelFormat.ReadFormat(reader),
                Caps1 = reader.ReadInt32(),
                Caps2 = reader.ReadInt32(),
                Caps3 = reader.ReadInt32(),
                Caps4 = reader.ReadInt32(),
                Reserved = reader.ReadInt32(),
            };
        }
    }
}