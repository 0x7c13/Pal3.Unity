// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataLoader
{
    using DataReader.Bmp;
    using UnityEngine;

    public class BmpTextureLoader : ITextureLoader
    {
        public Texture2D LoadTexture(byte[] data, out bool hasAlphaChannel)
        {
            hasAlphaChannel = false;
            return new BmpLoader().LoadBmp(data).ToTexture2D();
        }
    }
}