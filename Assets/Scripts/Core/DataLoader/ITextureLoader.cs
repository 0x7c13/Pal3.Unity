// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataLoader
{
    using UnityEngine;

    public interface ITextureLoader
    {
        Texture2D LoadTexture(byte[] data, out bool hasAlphaChannel);
    }
}