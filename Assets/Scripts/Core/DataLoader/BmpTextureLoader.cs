// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataLoader
{
    using DataReader.Bmp;
    using UnityEngine;

    public class BmpTextureLoader : ITextureLoader
    {
        private BMPImage _image;

        public void Load(byte[] data, out bool hasAlphaChannel)
        {
            hasAlphaChannel = false;
            _image = new BmpLoader().LoadBmp(data);
        }

        public Texture2D ToTexture2D()
        {
            return _image?.ToTexture2D();
        }
    }
}