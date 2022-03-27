// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataLoader
{
    using UnityEngine;

    public interface ITextureLoader
    {
        void Load(byte[] data, out bool hasAlphaChannel);

        Texture2D ToTexture2D();
    }
}