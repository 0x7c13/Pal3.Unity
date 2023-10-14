// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Core.Abstraction
{
    /// <summary>
    /// Texture loader and ITexture2D creator interface.
    /// The reason we have a separate Load method interface is because
    /// uploading texture to GPU is single threaded (upload texture to VRAM),
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
        public void Load(byte[] data, out bool hasAlphaChannel);

        /// <summary>
        /// Convert loaded data into ITexture2D instance.
        /// </summary>
        /// <returns>ITexture2D instance</returns>
        public ITexture2D ToTexture();
    }
}