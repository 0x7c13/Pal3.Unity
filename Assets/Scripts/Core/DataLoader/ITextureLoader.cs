// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataLoader
{
    using UnityEngine;

    /// <summary>
    /// Texture loader and Texture2D converter interface.
    /// The reason we have a separate Load method interface is because
    /// Texture2D.Apply() is single threaded (upload texture to VRAM),
    /// while the loading and decoding part can run in parallel and it should
    /// be thread-safe in general. This two-step approach gives caller
    /// more flexibility.
    /// </summary>
    public interface ITextureLoader
    {
        /// <summary>
        /// Load and decode texture data.
        /// </summary>
        /// <param name="data">Raw texture data in byte array</param>
        /// <param name="hasAlphaChannel">True if texture has alpha channel</param>
        void Load(byte[] data, out bool hasAlphaChannel);

        /// <summary>
        /// Convert loaded texture into Texture2D instance.
        /// </summary>
        /// <returns>Texture2D instance</returns>
        Texture2D ToTexture2D();
    }
}