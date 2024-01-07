// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Core.Abstraction
{
    /// <summary>
    /// Interface for providing texture resources.
    /// </summary>
    public interface ITextureResourceProvider
    {
        /// <summary>
        /// Gets the path of the texture with the specified name.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <returns>The path of the texture.</returns>
        public string GetTexturePath(string name);

        /// <summary>
        /// Gets the texture with the specified name.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <returns>The texture.</returns>
        public ITexture2D GetTexture(string name);

        /// <summary>
        /// Gets the texture with the specified name and determines if it has an alpha channel.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <param name="hasAlphaChannel">Whether or not the texture has an alpha channel.</param>
        /// <returns>The texture.</returns>
        public ITexture2D GetTexture(string name, out bool hasAlphaChannel);
    }
}