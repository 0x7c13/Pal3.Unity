// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Core.Abstraction
{
    /// <summary>
    /// Specifies how texture coordinates outside the range of 0 to 1 are handled.
    /// </summary>
    public enum WrapMode
    {
        /// <summary>
        /// Tiles the texture, creating a repeating pattern.
        /// </summary>
        Repeat,

        /// <summary>
        /// Clamps the texture to the last pixel at the edge, creating a solid color border.
        /// </summary>
        Clamp,
    }

    /// <summary>
    /// Interface for a 2D texture.
    /// </summary>
    public interface ITexture2D : IManagedObject
    {
        /// <summary>
        /// The width of the texture.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// The height of the texture.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Sets the wrap mode of the texture.
        /// </summary>
        /// <param name="wrapMode">The wrap mode to set.</param>
        public void SetWrapMode(WrapMode wrapMode);

        /// <summary>
        /// Gets the pixel color at the specified position.
        /// </summary>
        /// <param name="x">The x-coordinate of the pixel.</param>
        /// <param name="y">The y-coordinate of the pixel.</param>
        /// <returns>The color of the pixel.</returns>
        public (float r, float g, float b, float a) GetPixel(int x, int y);

        /// <summary>
        /// Creates a sprite from the texture.
        /// </summary>
        /// <param name="x">The x-coordinate of the sprite.</param>
        /// <param name="y">The y-coordinate of the sprite.</param>
        /// <param name="width">The width of the sprite.</param>
        /// <param name="height">The height of the sprite.</param>
        /// <param name="pivotX">The x-coordinate of the pivot point.</param>
        /// <param name="pivotY">The y-coordinate of the pivot point.</param>
        /// <returns>The created sprite.</returns>
        public ISprite CreateSprite(
            float x, float y,
            float width, float height,
            float pivotX, float pivotY);
    }
}