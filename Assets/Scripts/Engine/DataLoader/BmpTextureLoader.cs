// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.DataLoader
{
    using System;
    using System.Buffers;
    using Core.Abstraction;
    using Pal3.Core.DataReader.Bmp;

    /// <summary>
    /// .bmp file loader and Texture2D converter.
    /// </summary>
    public sealed class BmpTextureLoader : ITextureLoader
    {
        private readonly ITextureFactory _textureFactory;
        private BmpFile _image;

        public BmpTextureLoader(ITextureFactory textureFactory)
        {
            _textureFactory = textureFactory;
        }

        public void Load(byte[] data, out bool hasAlphaChannel)
        {
            if (_image != null) throw new Exception("BMP texture already loaded");

            hasAlphaChannel = false;
            _image = new BmpFileReader().Read(data);
        }

        // Flip image if height is negative
        private void FlipImage()
        {
            if (_image.Info.Height > 0) return;

            int width = _image.Info.AbsWidth;
            int height = _image.Info.AbsHeight;

            for (int y = 0; y < height / 2; y++)
            {
                for(int x = 0, o1 = y * width, o2 = (height - y - 1) * width; x < width; x++, o1++, o2++)
                {
                    (_image.ImageData[o1], _image.ImageData[o2]) = (_image.ImageData[o2], _image.ImageData[o1]);
                }
            }

            _image.Info.Height = height;
        }

        public ITexture2D ToTexture()
        {
            if (_image == null) return null;

            if (_image.Info.Height < 0)
            {
                FlipImage();
            }

            byte[] rawRgbaDataBuffer = ArrayPool<byte>.Shared.Rent(_image.ImageData.Length * 4);

            try
            {
                for (int i = 0; i < _image.ImageData.Length; i++)
                {
                    rawRgbaDataBuffer[i * 4 + 0] = _image.ImageData[i].B;
                    rawRgbaDataBuffer[i * 4 + 1] = _image.ImageData[i].G;
                    rawRgbaDataBuffer[i * 4 + 2] = _image.ImageData[i].R;
                    rawRgbaDataBuffer[i * 4 + 3] = _image.ImageData[i].A;
                }

                // Use the buffer instead of a new array
                return _textureFactory.CreateTexture(_image.Info.AbsWidth, _image.Info.AbsHeight, rawRgbaDataBuffer);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rawRgbaDataBuffer);
                _image = null;
            }
        }
    }
}