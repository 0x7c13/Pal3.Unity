// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Core.Abstraction
{
    /// <summary>
    /// Represents a factory for creating materials.
    /// </summary>
    public interface IMaterialFactory
    {
        /// <summary>
        /// Creates a new material with the specified shader name.
        /// </summary>
        /// <param name="shaderName">The name of the shader to use for the material.</param>
        /// <returns>The created material.</returns>
        public IMaterial CreateMaterial(string shaderName);

        /// <summary>
        /// Creates a new material based on an existing material.
        /// </summary>
        /// <param name="material">The existing material to create a new material from.</param>
        /// <returns>The created material.</returns>
        public IMaterial CreateMaterialFrom(IMaterial material);
    }
}