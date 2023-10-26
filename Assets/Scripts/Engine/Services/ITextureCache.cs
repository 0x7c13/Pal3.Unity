// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Services
{
    using Core.Abstraction;

    /// <summary>
    /// Interface for a texture cache that stores and retrieves textures by a string key.
    /// </summary>
    public interface ITextureCache
    {
        /// <summary>
        /// Adds a texture to the cache with the specified key.
        /// </summary>
        /// <param name="key">The key to associate with the texture.</param>
        /// <param name="texture">The texture to add to the cache.</param>
        /// <param name="hasAlphaChannel">Whether the texture has an alpha channel.</param>
        public void Add(string key, ITexture2D texture, bool hasAlphaChannel);

        /// <summary>
        /// Attempts to retrieve a texture from the cache with the specified key.
        /// </summary>
        /// <param name="key">The key associated with the texture.</param>
        /// <param name="texture">The retrieved texture, if found.</param>
        /// <returns>True if the texture was found in the cache, false otherwise.</returns>
        public bool TryGet(string key, out (ITexture2D texture, bool hasAlphaChannel) texture);

        /// <summary>
        /// Disposes all textures in the cache.
        /// </summary>
        public void DisposeAll();
    }
}