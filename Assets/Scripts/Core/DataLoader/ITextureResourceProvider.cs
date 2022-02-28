// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataLoader
{
    using UnityEngine;

    public interface ITextureResourceProvider
    {
        Texture2D GetTexture(string name);
        
        Texture2D GetTexture(string name, out bool hasAlphaChannel);
    }
}