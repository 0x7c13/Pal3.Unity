// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataLoader
{
    using DataReader.Bmp;
    using UnityEngine;

    /// <summary>
    /// .bmp file loader and Texture2D converter.
    /// </summary>
    public sealed class BmpTextureLoader : ITextureLoader
    {
        private BmpFile _image;

        public void Load(byte[] data, out bool hasAlphaChannel)
        {
            hasAlphaChannel = false;
            _image = new BmpFileReader().Read(data);
        }

        public Texture2D ToTexture2D()
        {
            if (_image == null) return null;

            var texture = new Texture2D(_image.Info.AbsWidth,
                _image.Info.AbsHeight,
                TextureFormat.RGBA32,
                mipChain: false);

            if (_image.Info.Height < 0)
            {
                FlipImage();
            }

            texture.SetPixels32(_image.ImageData);
            texture.Apply();
            return texture;
        }

        // flip image if height is negative
        private void FlipImage()
        {
            if (_image.Info.Height > 0)
                return;
            int w = _image.Info.AbsWidth;
            int h = _image.Info.AbsHeight;
            int h2 = h / 2;
            for (int y = 0; y < h2; y++)
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