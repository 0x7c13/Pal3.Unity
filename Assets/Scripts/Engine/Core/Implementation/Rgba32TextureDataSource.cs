// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Engine.Core.Implementation
{
    using System;
    using System.Buffers;
    using Abstraction;

    /// <summary>A base-level RGBA32 upload buffer, optionally owned by the shared array pool.</summary>
    public sealed class Rgba32TextureDataSource : ITextureDataSource
    {
        public int Width { get; }
        public int Height { get; }
        public int MipCount => 1;
        public TextureDataFormat Format => TextureDataFormat.Rgba32;
        public bool HasTransparency { get; }

        private byte[] _data;
        private readonly int _length;
        private readonly bool _returnToPool;

        /// <summary>Borrow a buffer; the caller must keep it unchanged until upload completes.</summary>
        public Rgba32TextureDataSource(int width, int height, byte[] data, bool hasTransparency = true)
            : this(width, height, data, hasTransparency, returnToPool: false) { }

        private Rgba32TextureDataSource(int width, int height, byte[] data,
            bool hasTransparency, bool returnToPool)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            _length = checked(width * height * 4);
            if (data == null || data.Length < _length) throw new ArgumentException("Incomplete RGBA32 data", nameof(data));
            Width = width;
            Height = height;
            HasTransparency = hasTransparency;
            _data = data;
            _returnToPool = returnToPool;
        }

        /// <summary>Transfer a rented buffer to this source, which returns it on Dispose.</summary>
        internal static Rgba32TextureDataSource TakePooledBuffer(int width, int height, byte[] data,
            bool hasTransparency) => new(width, height, data, hasTransparency, returnToPool: true);

        public ArraySegment<byte> GetMipData(int mip)
        {
            if (mip != 0) throw new ArgumentOutOfRangeException(nameof(mip));
            if (_data == null) throw new ObjectDisposedException(nameof(Rgba32TextureDataSource));
            return new ArraySegment<byte>(_data, 0, _length);
        }

        public void CopyMipToRgba32(int mip, byte[] destination)
        {
            var data = GetMipData(mip);
            Buffer.BlockCopy(data.Array, data.Offset, destination, 0, data.Count);
        }

        public ITexturePixelReader CreatePixelReader()
        {
            var data = GetMipData(0);
            var ownedPixels = new byte[data.Count];
            Buffer.BlockCopy(data.Array, data.Offset, ownedPixels, 0, data.Count);
            return new PixelReader(Width, Height, ownedPixels);
        }

        public void Dispose()
        {
            if (_data == null) return;
            if (_returnToPool) ArrayPool<byte>.Shared.Return(_data);
            _data = null;
        }

        private sealed class PixelReader : ITexturePixelReader
        {
            private readonly int _width;
            private readonly int _height;
            private readonly byte[] _pixels;

            public PixelReader(int width, int height, byte[] pixels)
            {
                _width = width;
                _height = height;
                _pixels = pixels;
            }

            public (byte r, byte g, byte b, byte a) GetPixel(int x, int y)
            {
                if ((uint)x >= (uint)_width || (uint)y >= (uint)_height)
                    throw new ArgumentOutOfRangeException(nameof(x));
                int offset = (y * _width + x) * 4;
                return (_pixels[offset], _pixels[offset + 1], _pixels[offset + 2], _pixels[offset + 3]);
            }
        }
    }
}
