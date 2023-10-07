#region License and Information
/*****
*
* BmpLoader.cs
*
* This is a simple implementation of a BMP file loader for Unity3D.
* Formats it should support are:
*  - 1 bit monochrome indexed
*  - 2-8 bit indexed
*  - 16 / 24 / 32 bit color (including "BI_BITFIELDS")
*  - RLE-4 and RLE-8 support has been added.
*
* Unless the type is "BI_ALPHABITFIELDS" the loader does not interpret alpha
* values by default, however you can set the "ReadPaletteAlpha" setting to
* true to interpret the 4th (usually "00") value as alpha for indexed images.
* You can also set "ForceAlphaReadWhenPossible" to true so it will interpret
* the "left over" bits as alpha if there are any. It will also force to read
* alpha from a palette if it's an indexed image, just like "ReadPaletteAlpha".
*
* It's not tested well to the bone, so there might be some errors somewhere.
* However I tested it with 4 different images created with MS Paint
* (1bit, 4bit, 8bit, 24bit) as those are the only formats supported.
*
* 2017.02.05 - first version
* 2017.03.06 - Added RLE4 / RLE8 support
* 2021.01.21 - Fixed RLE4 bug; Fixed wrongly reading bit masks for indexed images.
* 2021.01.22 - Addes support for negative heights (top-down images) The actual
*              flipping happens once at the Texture2D conversion.
*
* Copyright (c) 2017 Markus Göbel (Bunny83)
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to
* deal in the Software without restriction, including without limitation the
* rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
* sell copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
* FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
* IN THE SOFTWARE.
*
*****/
#endregion License and Information

namespace Pal3.Core.DataReader.Bmp
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Primitives;

    public enum BmpCompressionMode : int
    {
        BI_RGB = 0x00,
        BI_RLE8 = 0x01,
        BI_RLE4 = 0x02,
        BI_BITFIELDS = 0x03,
        BI_JPEG = 0x04,
        BI_PNG = 0x05,
        BI_ALPHABITFIELDS = 0x06,

        BI_CMYK = 0x0B,
        BI_CMYKRLE8 = 0x0C,
        BI_CMYKRLE4 = 0x0D,
    }

    public struct BmpFileHeader
    {
        public ushort Magic; // "BM"
        public uint Filesize;
        public uint Reserved;
        public uint Offset;
    }

    public struct BitmapInfoHeader
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort NColorPlanes; // always 1
        public ushort NBitsPerPixel; // [1,4,8,16,24,32]
        public BmpCompressionMode CompressionMode;
        public uint RawImageSize; // can be "0"
        public int XPpm;
        public int YPpm;
        public uint NPaletteColors;
        public uint NImportantColors;

        public int AbsWidth => Math.Abs(Width);
        public int AbsHeight => Math.Abs(Height);
    }

    public sealed class BmpFileReader
    {
        private const ushort MAGIC = 0x4D42; // "BM" little endian

        private bool READ_PALETTE_ALPHA = false;
        private bool FORCE_ALPHA_READ_WHEN_POSSIBLE = false;

        public BmpFile Read(string aFileName)
        {
            using FileStream file = File.OpenRead(aFileName);
            return Read(file);
        }

        public BmpFile Read(byte[] aData)
        {
            using var stream = new MemoryStream(aData);
            return Read(stream);
        }

        public BmpFile Read(Stream aData)
        {
            using var reader = new BinaryReader(aData);
            return Read(reader);
        }

        public BmpFile Read(BinaryReader aReader)
        {
            BmpFile bmp = new BmpFile();
            if (!ReadFileHeader(aReader, ref bmp.Header))
            {
                throw new InvalidDataException("Not a valid BMP file");
            }
            if (!ReadInfoHeader(aReader, ref bmp.Info))
            {
                throw new InvalidDataException("Unsupported header format");
            }
            if (bmp.Info.CompressionMode != BmpCompressionMode.BI_RGB
                && bmp.Info.CompressionMode != BmpCompressionMode.BI_BITFIELDS
                && bmp.Info.CompressionMode != BmpCompressionMode.BI_ALPHABITFIELDS
                && bmp.Info.CompressionMode != BmpCompressionMode.BI_RLE4
                && bmp.Info.CompressionMode != BmpCompressionMode.BI_RLE8
                )
            {
                throw new NotSupportedException("Unsupported image format: " + bmp.Info.CompressionMode);
            }
            long offset = 14 + bmp.Info.Size;
            aReader.BaseStream.Seek(offset, SeekOrigin.Begin);
            if (bmp.Info.NBitsPerPixel < 24)
            {
                bmp.RMask = 0x00007C00;
                bmp.GMask = 0x000003E0;
                bmp.BMask = 0x0000001F;
            }

            if (bmp.Info.NBitsPerPixel > 8 && (bmp.Info.CompressionMode == BmpCompressionMode.BI_BITFIELDS || bmp.Info.CompressionMode == BmpCompressionMode.BI_ALPHABITFIELDS))
            {
                bmp.RMask = aReader.ReadUInt32();
                bmp.GMask = aReader.ReadUInt32();
                bmp.BMask = aReader.ReadUInt32();
            }
            if (FORCE_ALPHA_READ_WHEN_POSSIBLE)
                bmp.AMask = GetMask(bmp.Info.NBitsPerPixel) ^ (bmp.RMask | bmp.GMask | bmp.BMask);

            if (bmp.Info.CompressionMode == BmpCompressionMode.BI_ALPHABITFIELDS)
                bmp.AMask = aReader.ReadUInt32();

            if (bmp.Info.NPaletteColors > 0 || bmp.Info.NBitsPerPixel <= 8)
                bmp.Palette = ReadPalette(aReader, bmp, READ_PALETTE_ALPHA || FORCE_ALPHA_READ_WHEN_POSSIBLE);


            aReader.BaseStream.Seek(bmp.Header.Offset, SeekOrigin.Begin);
            bool uncompressed = bmp.Info.CompressionMode == BmpCompressionMode.BI_RGB ||
                bmp.Info.CompressionMode == BmpCompressionMode.BI_BITFIELDS ||
                bmp.Info.CompressionMode == BmpCompressionMode.BI_ALPHABITFIELDS;
            if (bmp.Info.NBitsPerPixel == 32 && uncompressed)
                Read32BitImage(aReader, bmp);
            else if (bmp.Info.NBitsPerPixel == 24 && uncompressed)
                Read24BitImage(aReader, bmp);
            else if (bmp.Info.NBitsPerPixel == 16 && uncompressed)
                Read16BitImage(aReader, bmp);
            else if (bmp.Info.CompressionMode == BmpCompressionMode.BI_RLE4 && bmp.Info.NBitsPerPixel == 4 && bmp.Palette != null)
                ReadIndexedImageRLE4(aReader, bmp);
            else if (bmp.Info.CompressionMode == BmpCompressionMode.BI_RLE8 && bmp.Info.NBitsPerPixel == 8 && bmp.Palette != null)
                ReadIndexedImageRLE8(aReader, bmp);
            else if (uncompressed && bmp.Info.NBitsPerPixel <= 8 && bmp.Palette != null)
                ReadIndexedImage(aReader, bmp);
            else
            {
                throw new NotSupportedException("Unsupported file format: " + bmp.Info.CompressionMode + " BPP: " + bmp.Info.NBitsPerPixel);
            }
            return bmp;
        }


        private static void Read32BitImage(BinaryReader aReader, BmpFile bmp)
        {
            int w = Math.Abs(bmp.Info.Width);
            int h = Math.Abs(bmp.Info.Height);
            Color32[] data = bmp.ImageData = new Color32[w * h];
            if (aReader.BaseStream.Position + w * h * 4 > aReader.BaseStream.Length)
            {
                throw new InvalidDataException("Unexpected end of file");
            }
            int shiftR = GetShiftCount(bmp.RMask);
            int shiftG = GetShiftCount(bmp.GMask);
            int shiftB = GetShiftCount(bmp.BMask);
            int shiftA = GetShiftCount(bmp.AMask);
            byte a = 255;
            for (int i = 0; i < data.Length; i++)
            {
                uint v = aReader.ReadUInt32();
                byte r = (byte)((v & bmp.RMask) >> shiftR);
                byte g = (byte)((v & bmp.GMask) >> shiftG);
                byte b = (byte)((v & bmp.BMask) >> shiftB);
                if (bmp.BMask != 0)
                    a = (byte)((v & bmp.AMask) >> shiftA);
                data[i] = new Color32(r, g, b, a);
            }
        }

        private static void Read24BitImage(BinaryReader aReader, BmpFile bmp)
        {
            int w = Math.Abs(bmp.Info.Width);
            int h = Math.Abs(bmp.Info.Height);
            int rowLength = ((24 * w + 31) / 32) * 4;
            int count = rowLength * h;
            int pad = rowLength - w * 3;
            Color32[] data = bmp.ImageData = new Color32[w * h];
            if (aReader.BaseStream.Position + count > aReader.BaseStream.Length)
            {
                throw new InvalidDataException("Unexpected end of file. (Have " +
                                               (aReader.BaseStream.Position + count) + " bytes, expected " +
                                               aReader.BaseStream.Length + " bytes)");
            }
            int shiftR = GetShiftCount(bmp.RMask);
            int shiftG = GetShiftCount(bmp.GMask);
            int shiftB = GetShiftCount(bmp.BMask);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    uint v = aReader.ReadByte() | ((uint)aReader.ReadByte() << 8) | ((uint)aReader.ReadByte() << 16);
                    byte r = (byte)((v & bmp.RMask) >> shiftR);
                    byte g = (byte)((v & bmp.GMask) >> shiftG);
                    byte b = (byte)((v & bmp.BMask) >> shiftB);
                    data[x + y * w] = new Color32(r, g, b, 255);
                }
                for (int i = 0; i < pad; i++)
                    aReader.ReadByte();
            }
        }

        private static void Read16BitImage(BinaryReader aReader, BmpFile bmp)
        {
            int w = Math.Abs(bmp.Info.Width);
            int h = Math.Abs(bmp.Info.Height);
            int rowLength = ((16 * w + 31) / 32) * 4;
            int count = rowLength * h;
            int pad = rowLength - w * 2;
            Color32[] data = bmp.ImageData = new Color32[w * h];
            if (aReader.BaseStream.Position + count > aReader.BaseStream.Length)
            {
                throw new InvalidDataException("Unexpected end of file. (Have " +
                                               (aReader.BaseStream.Position + count) + " bytes, expected " +
                                               aReader.BaseStream.Length + " bytes)");
            }
            int shiftR = GetShiftCount(bmp.RMask);
            int shiftG = GetShiftCount(bmp.GMask);
            int shiftB = GetShiftCount(bmp.BMask);
            int shiftA = GetShiftCount(bmp.AMask);
            byte a = 255;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    uint v = aReader.ReadByte() | ((uint)aReader.ReadByte() << 8);
                    byte r = (byte)((v & bmp.RMask) >> shiftR);
                    byte g = (byte)((v & bmp.GMask) >> shiftG);
                    byte b = (byte)((v & bmp.BMask) >> shiftB);
                    if (bmp.AMask != 0)
                        a = (byte)((v & bmp.AMask) >> shiftA);
                    data[x + y * w] = new Color32(r, g, b, a);
                }
                for (int i = 0; i < pad; i++)
                    aReader.ReadByte();
            }
        }

        private static void ReadIndexedImage(BinaryReader aReader, BmpFile bmp)
        {
            int w = Math.Abs(bmp.Info.Width);
            int h = Math.Abs(bmp.Info.Height);
            int bitCount = bmp.Info.NBitsPerPixel;
            int rowLength = ((bitCount * w + 31) / 32) * 4;
            int count = rowLength * h;
            int pad = rowLength - (w * bitCount + 7) / 8;
            Color32[] data = bmp.ImageData = new Color32[w * h];
            if (aReader.BaseStream.Position + count > aReader.BaseStream.Length)
            {
                throw new InvalidDataException("Unexpected end of file. (Have " +
                                               (aReader.BaseStream.Position + count) + " bytes, expected " +
                                               aReader.BaseStream.Length + " bytes)");
            }
            BitStreamReader bitReader = new BitStreamReader(aReader);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int v = (int)bitReader.ReadBits(bitCount);
                    if (v >= bmp.Palette.Count)
                    {
                        throw new InvalidDataException("Indexed bitmap has indices greater than it's color palette");
                    }
                    data[x + y * w] = bmp.Palette[v];
                }
                bitReader.Flush();
                for (int i = 0; i < pad; i++)
                    aReader.ReadByte();
            }
        }

        private static void ReadIndexedImageRLE4(BinaryReader aReader, BmpFile bmp)
        {
            int w = Math.Abs(bmp.Info.Width);
            int h = Math.Abs(bmp.Info.Height);
            Color32[] data = bmp.ImageData = new Color32[w * h];
            int x = 0;
            int y = 0;
            int yOffset = 0;
            while (aReader.BaseStream.Position < aReader.BaseStream.Length - 1)
            {
                int count = (int)aReader.ReadByte();
                byte d = aReader.ReadByte();
                if (count > 0)
                {
                    for (int i = (count / 2); i > 0; i--)
                    {
                        data[x++ + yOffset] = bmp.Palette[(d >> 4) & 0x0F];
                        data[x++ + yOffset] = bmp.Palette[d & 0x0F];
                    }
                    if ((count & 0x01) > 0)
                    {
                        data[x++ + yOffset] = bmp.Palette[(d >> 4) & 0x0F];
                    }
                }
                else
                {
                    if (d == 0)
                    {
                        x = 0;
                        y += 1;
                        yOffset = y * w;
                    }
                    else if (d == 1)
                    {
                        break;
                    }
                    else if (d == 2)
                    {
                        x += aReader.ReadByte();
                        y += aReader.ReadByte();
                        yOffset = y * w;
                    }
                    else
                    {
                        for (int i = (d / 2); i > 0; i--)
                        {
                            byte d2 = aReader.ReadByte();
                            data[x++ + yOffset] = bmp.Palette[(d2 >> 4) & 0x0F];
                            if (x + 1 < w)
                                data[x++ + yOffset] = bmp.Palette[d2 & 0x0F];
                        }
                        if ((d & 0x01) > 0)
                        {
                            data[x++ + yOffset] = bmp.Palette[(aReader.ReadByte() >> 4) & 0x0F];
                        }
                        if ((((d - 1) / 2) & 1) == 0)
                        {
                            aReader.ReadByte(); // padding (word alignment)
                        }
                    }
                }
            }
        }

        private static void ReadIndexedImageRLE8(BinaryReader aReader, BmpFile bmp)
        {
            int w = Math.Abs(bmp.Info.Width);
            int h = Math.Abs(bmp.Info.Height);
            Color32[] data = bmp.ImageData = new Color32[w * h];
            int x = 0;
            int y = 0;
            int yOffset = 0;
            while (aReader.BaseStream.Position < aReader.BaseStream.Length - 1)
            {
                int count = (int)aReader.ReadByte();
                byte d = aReader.ReadByte();
                if (count > 0)
                {
                    for (int i = count; i > 0; i--)
                    {
                        data[x++ + yOffset] = bmp.Palette[d];
                    }
                }
                else
                {
                    if (d == 0)
                    {
                        x = 0;
                        y += 1;
                        yOffset = y * w;
                    }
                    else if (d == 1)
                    {
                        break;
                    }
                    else if (d == 2)
                    {
                        x += aReader.ReadByte();
                        y += aReader.ReadByte();
                        yOffset = y * w;
                    }
                    else
                    {
                        for (int i = d; i > 0; i--)
                        {
                            data[x++ + yOffset] = bmp.Palette[aReader.ReadByte()];
                        }
                        if ((d & 0x01) > 0)
                        {
                            aReader.ReadByte(); // padding (word alignment)
                        }
                    }
                }
            }
        }

        private static int GetShiftCount(uint mask)
        {
            for (int i = 0; i < 32; i++)
            {
                if ((mask & 0x01) > 0)
                    return i;
                mask >>= 1;
            }
            return -1;
        }

        private static uint GetMask(int bitCount)
        {
            uint mask = 0;
            for (int i = 0; i < bitCount; i++)
            {
                mask <<= 1;
                mask |= 0x01;
            }
            return mask;
        }

        private static bool ReadFileHeader(BinaryReader aReader, ref BmpFileHeader aFileHeader)
        {
            aFileHeader.Magic = aReader.ReadUInt16();
            if (aFileHeader.Magic != MAGIC)
                return false;
            aFileHeader.Filesize = aReader.ReadUInt32();
            aFileHeader.Reserved = aReader.ReadUInt32();
            aFileHeader.Offset = aReader.ReadUInt32();
            return true;
        }

        private static bool ReadInfoHeader(BinaryReader aReader, ref BitmapInfoHeader aHeader)
        {
            aHeader.Size = aReader.ReadUInt32();
            if (aHeader.Size < 40)
                return false;
            aHeader.Width = aReader.ReadInt32();
            aHeader.Height = aReader.ReadInt32();
            aHeader.NColorPlanes = aReader.ReadUInt16();
            aHeader.NBitsPerPixel = aReader.ReadUInt16();
            aHeader.CompressionMode = (BmpCompressionMode)aReader.ReadInt32();
            aHeader.RawImageSize = aReader.ReadUInt32();
            aHeader.XPpm = aReader.ReadInt32();
            aHeader.YPpm = aReader.ReadInt32();
            aHeader.NPaletteColors = aReader.ReadUInt32();
            aHeader.NImportantColors = aReader.ReadUInt32();
            int pad = (int)aHeader.Size - 40;
            if (pad > 0)
                aReader.ReadBytes(pad);
            return true;
        }

        public static List<Color32> ReadPalette(BinaryReader aReader, BmpFile aBmp, bool aReadAlpha)
        {
            uint count = aBmp.Info.NPaletteColors;
            if (count == 0u)
                count = 1u << aBmp.Info.NBitsPerPixel;
            var palette = new List<Color32>((int)count);
            for (int i = 0; i < count; i++)
            {
                byte b = aReader.ReadByte();
                byte g = aReader.ReadByte();
                byte r = aReader.ReadByte();
                byte a = aReader.ReadByte();
                if (!aReadAlpha)
                    a = 255;
                palette.Add(new Color32(r, g, b, a));
            }
            return palette;
        }

    }

    internal class BitStreamReader
    {
        private readonly BinaryReader _mReader;

        private byte _mData = 0;
        private int _mBits = 0;

        public BitStreamReader(BinaryReader aReader)
        {
            _mReader = aReader;
        }

        public BitStreamReader(Stream aStream) : this(new BinaryReader(aStream)) { }

        public byte ReadBit()
        {
            if (_mBits <= 0)
            {
                _mData = _mReader.ReadByte();
                _mBits = 8;
            }
            return (byte)((_mData >> --_mBits) & 1);
        }

        public ulong ReadBits(int aCount)
        {
            if (aCount <= 0 || aCount > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(aCount), "aCount must be between 1 and 32 inclusive");
            }

            ulong val = 0UL;

            for (var i = aCount - 1; i >= 0; i--)
            {
                val |= ((ulong)ReadBit() << i);
            }

            return val;
        }

        public void Flush()
        {
            _mData = 0;
            _mBits = 0;
        }
    }
}