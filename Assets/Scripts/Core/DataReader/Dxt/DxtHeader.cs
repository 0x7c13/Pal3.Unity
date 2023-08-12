// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Dxt
{
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

        public static DxtPixelFormat ReadFormat(IBinaryReader reader)
        {
            return new DxtPixelFormat
            {
                Size = reader.ReadInt32(),
                Flags = reader.ReadInt32(),
                Format = reader.ReadString(4),
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
        public int[] ReservedBytes; // 11 ints
        public DxtPixelFormat DxtPixelFormat;
        public int Caps1;
        public int Caps2;
        public int Caps3;
        public int Caps4;
        public int Reserved;

        public static DxtHeader ReadHeader(IBinaryReader reader)
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
                ReservedBytes = reader.ReadInt32s(11),
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