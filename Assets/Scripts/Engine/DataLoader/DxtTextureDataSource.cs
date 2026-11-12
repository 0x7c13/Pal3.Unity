// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Engine.DataLoader
{
    using System;
    using Core.Abstraction;
    using Pal3.Core.DataReader.Dxt;

    /// <summary>Adapts validated DDS data to format-neutral texture blocks and pixel reads.</summary>
    internal sealed class DxtTextureDataSource : ITextureDataSource, ITexturePixelReader
    {
        private readonly DxtImage _image;

        public DxtTextureDataSource(DxtImage image) => _image = image;

        public int Width => _image.Width;
        public int Height => _image.Height;
        public int MipCount => _image.MipCount;
        public TextureDataFormat Format => _image.HasAlphaChannel ? TextureDataFormat.Bc2 : TextureDataFormat.Bc1;
        public bool HasTransparency => _image.HasTransparency;

        public ArraySegment<byte> GetMipData(int mip)
        {
            int offset = _image.GetOffset(mip);
            int length = ((_image.GetWidth(mip) + 3) / 4) * ((_image.GetHeight(mip) + 3) / 4) *
                (Format == TextureDataFormat.Bc2 ? 16 : 8);
            return new ArraySegment<byte>(_image.Data, offset, length);
        }

        public void CopyMipToRgba32(int mip, byte[] destination) => _image.Decode(mip, destination);

        // DDS bytes are owned managed data, never a pooled upload buffer. Sharing this reader
        // keeps compressed blocks alive without allocating an uncompressed CPU copy.
        public ITexturePixelReader CreatePixelReader() => this;

        public (byte r, byte g, byte b, byte a) GetPixel(int x, int y) => _image.GetPixel(x, y);

        public void Dispose() { }
    }
}
