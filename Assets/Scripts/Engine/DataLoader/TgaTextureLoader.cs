// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.DataLoader
{
    using System;
    using System.Buffers;
    using Core.Abstraction;
    using Pal3.Core.DataReader.Tga;

    /// <summary>
    /// .tga file loader and Texture2D converter.
    /// </summary>
    public sealed class TgaTextureLoader : ITextureLoader
    {
        private readonly ITextureFactory _textureFactory;

        private short _width;
        private short _height;
        private byte[] _rawRgbaDataBuffer;

        public TgaTextureLoader(ITextureFactory textureFactory)
        {
            _textureFactory = textureFactory;
        }

        public unsafe void Load(byte[] data, out bool hasAlphaChannel)
        {
            if (_rawRgbaDataBuffer != null) throw new Exception("TGA texture already loaded");

            byte bitDepth;

            fixed (byte* p = &data[12])
            {
                _width = *(short*)p;
                _height = *(short*)(p + 2);
                bitDepth = *(p + 4);
            }

            _rawRgbaDataBuffer = ArrayPool<byte>.Shared.Rent(_width * _height * 4);

            switch (bitDepth)
            {
                case 24:
                    hasAlphaChannel = false;
                    TgaDecoder.Decode24BitDataToRgba32(
                        data, _width, _height, _rawRgbaDataBuffer);
                    break;
                case 32:
                    TgaDecoder.Decode32BitDataToRgba32(
                        data, _width, _height, _rawRgbaDataBuffer, out hasAlphaChannel);
                    break;
                default:
                    throw new Exception("TGA texture had non 32/24 bit depth");
            }
        }

        public ITexture2D ToTexture()
        {
            if (_rawRgbaDataBuffer == null) return null;

            try
            {
                return _textureFactory.CreateTexture(_width, _height, _rawRgbaDataBuffer);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(_rawRgbaDataBuffer);
                _rawRgbaDataBuffer = null;
            }
        }
    }
}