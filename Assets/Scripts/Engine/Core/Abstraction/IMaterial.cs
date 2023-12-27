// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Core.Abstraction
{
    using Pal3.Core.Primitives;

    /// <summary>
    /// Represents a material used in rendering.
    /// </summary>
    public interface IMaterial : IManagedObject
    {
        /// <summary>
        /// Gets the name of the shader associated with this material.
        /// </summary>
        public string ShaderName { get; }

        /// <summary>
        /// Gets the float value of a material property.
        /// </summary>
        /// <param name="propertyId">The ID of the property.</param>
        /// <returns>The float value of the property.</returns>
        public float GetFloat(int propertyId);

        /// <summary>
        /// Gets the integer value of a material property.
        /// </summary>
        /// <param name="propertyId">The ID of the property.</param>
        /// <returns>The integer value of the property.</returns>
        public int GetInt(int propertyId);

        /// <summary>
        /// Sets the integer value of a material property.
        /// </summary>
        /// <param name="propertyId">The ID of the property.</param>
        /// <param name="value">The new integer value of the property.</param>
        public void SetInt(int propertyId, int value);

        /// <summary>
        /// Sets the float value of a material property.
        /// </summary>
        /// <param name="propertyId">The ID of the property.</param>
        /// <param name="value">The new float value of the property.</param>
        public void SetFloat(int propertyId, float value);

        /// <summary>
        /// Sets the color value of a material property.
        /// </summary>
        /// <param name="propertyId">The ID of the property.</param>
        /// <param name="value">The new color value of the property.</param>
        public void SetColor(int propertyId, Color value);

        /// <summary>
        /// Sets the main texture of the material.
        /// </summary>
        /// <param name="texture">The main texture to set.</param>
        public void SetMainTexture(ITexture2D texture);

        /// <summary>
        /// Sets a texture for a material property.
        /// </summary>
        /// <param name="propertyId">The ID of the property.</param>
        /// <param name="texture">The texture to set.</param>
        public void SetTexture(int propertyId, ITexture2D texture);

        /// <summary>
        /// Sets the scale of the main texture.
        /// </summary>
        /// <param name="x">The scale factor along the x-axis.</param>
        /// <param name="y">The scale factor along the y-axis.</param>
        public void SetMainTextureScale(float x, float y);
    }
}