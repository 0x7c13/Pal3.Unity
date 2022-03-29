// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataLoader
{
    using UnityEngine;

    /// <summary>
    /// Texture2D provider.
    /// </summary>
    public interface ITextureResourceProvider
    {
        Texture2D GetTexture(string name);
        
        Texture2D GetTexture(string name, out bool hasAlphaChannel);
    }
}