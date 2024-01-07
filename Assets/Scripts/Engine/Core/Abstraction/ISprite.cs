// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Core.Abstraction
{
    /// <summary>
    /// Interface for a sprite object.
    /// </summary>
    public interface ISprite : IManagedObject
    {
        /// <summary>
        /// The texture used by the sprite.
        /// </summary>
        public ITexture2D Texture { get; }
    }
}