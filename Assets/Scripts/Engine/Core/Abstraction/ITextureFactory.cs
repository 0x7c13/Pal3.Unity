// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Core.Abstraction
{
    /// <summary>Creates graphics textures from CPU-side pixel data.</summary>
    public interface ITextureFactory
    {
        /// <summary>Create a readable, base-only RGBA32 texture from a borrowed buffer.</summary>
        /// <param name="width">Positive texture width in pixels.</param>
        /// <param name="height">Positive texture height in pixels.</param>
        /// <param name="rgba32DataBuffer">At least width * height * 4 bytes in RGBA order.
        /// The caller may reuse the buffer after this call returns.</param>
        /// <returns>The created texture, owned by the caller.</returns>
        ITexture2D CreateTexture(int width, int height, byte[] rgba32DataBuffer);

        /// <summary>
        /// Synchronously upload a format-neutral data source using resolved creation options.
        /// The caller still owns the source and may dispose it after this call returns.
        /// </summary>
        /// <param name="source">Borrowed pixel data and mip layout; remains caller-owned.</param>
        /// <param name="options">Resolved mip, compression and CPU-readability requirements.</param>
        /// <returns>The created texture, which retains any required independent pixel reader.</returns>
        ITexture2D CreateTexture(ITextureDataSource source, TextureCreateOptions options);

        /// <summary>Create a new white texture.</summary>
        ITexture2D CreateWhiteTexture();
    }
}
