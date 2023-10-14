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

        public ITexture2D ToTexture()
        {
            if (_image == null) return null;

            if (_image.Info.Height < 0)
            {
                FlipImage();
            }

            byte[] rawRgbaData = ArrayPool<byte>.Shared.Rent(_image.ImageData.Length * 4);

            try
            {
                for (int i = 0; i < _image.ImageData.Length; i++)
                {
                    rawRgbaData[i * 4 + 0] = _image.ImageData[i].B;
                    rawRgbaData[i * 4 + 1] = _image.ImageData[i].G;
                    rawRgbaData[i * 4 + 2] = _image.ImageData[i].R;
                    rawRgbaData[i * 4 + 3] = _image.ImageData[i].A;
                }

                // Use the buffer instead of a new array
                return _textureFactory.CreateTexture(_image.Info.AbsWidth, _image.Info.AbsHeight, rawRgbaData);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rawRgbaData);
                _image = null;
            }
        }

        // flip image if height is negative
        private void FlipImage()
        {
            if (_image.Info.Height > 0) return;

            int w = _image.Info.AbsWidth;
            int h = _image.Info.AbsHeight;

            for (int y = 0; y < h / 2; y++)
            {
                for(int x = 0, o1=y*w, o2=(h-y-1)*w; x < w; x++,o1++,o2++)
                {
                    (_image.ImageData[o1], _image.ImageData[o2]) = (_image.ImageData[o2], _image.ImageData[o1]);
                }
            }

            _image.Info.Height = h;
        }
    }
}