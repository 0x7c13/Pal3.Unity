// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Core.Abstraction
{
    /// <summary>Decode a file into CPU-side data without creating graphics objects.</summary>
    public interface ITextureLoader
    {
        /// <summary>
        /// Load texture data. The caller disposes the returned source after uploading it.
        /// This operation may run off the graphics thread; loaders hold no per-image state.
        /// </summary>
        /// <param name="data">Owned file bytes, which must not be modified after loading.</param>
        /// <param name="hasAlphaChannel">Legacy material classification, independent of sampling policy.</param>
        ITextureDataSource Load(byte[] data, out bool hasAlphaChannel);
    }
}
