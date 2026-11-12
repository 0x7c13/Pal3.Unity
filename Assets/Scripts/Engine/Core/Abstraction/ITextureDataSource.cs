// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Engine.Core.Abstraction
{
    using System;

    public enum TextureDataFormat
    {
        Rgba32,
        Bc1,
        Bc2,
    }

    /// <summary>
    /// Texture pixels and mip layout, independent of a file container or graphics API.
    /// The caller owns this source and disposes it after synchronous texture creation.
    /// Upload buffers must remain unchanged until then and may be pooled.
    /// </summary>
    public interface ITextureDataSource : IDisposable
    {
        int Width { get; }
        int Height { get; }
        int MipCount { get; }
        TextureDataFormat Format { get; }
        /// <summary>Conservative indication that alpha coverage must be preserved.</summary>
        bool HasTransparency { get; }

        /// <summary>Borrow one tightly packed mip in Format encoding; do not modify its data.</summary>
        ArraySegment<byte> GetMipData(int mip);

        /// <summary>Copy or decode one mip to RGBA32 in a caller-owned buffer.</summary>
        void CopyMipToRgba32(int mip, byte[] destination);

        /// <summary>
        /// Create a reader whose backing data remains valid after this source is disposed.
        /// The texture retains the reader until Destroy; owned immutable data may be shared.
        /// </summary>
        ITexturePixelReader CreatePixelReader();
    }
}
