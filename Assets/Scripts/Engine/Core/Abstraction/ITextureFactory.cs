// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Core.Abstraction
{
    /// <summary>
    /// Interface for a texture factory that creates textures.
    /// </summary>
    public interface ITextureFactory
    {
        /// <summary>
        /// Creates a new texture with the specified width, height, and RGBA data.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="rgba32DataBuffer">The RGBA32 data of the texture to be read from.
        /// Size should be greater than or equal to width * height * 4.</param>
        /// <returns>The created texture.</returns>
        public ITexture2D CreateTexture(int width, int height, byte[] rgba32DataBuffer);

        /// <summary>
        /// Creates a new white texture.
        /// </summary>
        /// <returns>The created texture.</returns>
        public ITexture2D CreateWhiteTexture();
    }
}