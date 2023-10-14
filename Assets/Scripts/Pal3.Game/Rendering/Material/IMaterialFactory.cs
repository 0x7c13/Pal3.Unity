// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Material
{
    using Core.Primitives;
    using Engine.Core.Abstraction;
    using UnityEngine;
    using Color = Core.Primitives.Color;

    public enum MaterialShaderType
    {
        Unlit,
        Lit,
    }

    public interface IMaterialFactory
    {
        public MaterialShaderType ShaderType { get; }

        /// <summary>
        /// Create a material for effect sprite without alpha channel.
        /// </summary>
        /// <returns>Material</returns>
        public Material CreateOpaqueSpriteMaterial();

        /// <summary>
        /// Create a material for effect sprite without alpha channel.
        /// </summary>
        /// <param name="texture">Texture</param>
        /// <returns>Material</returns>
        public Material CreateOpaqueSpriteMaterial(ITexture2D texture);

        /// <summary>
        /// Create standard materials.
        /// </summary>
        /// <param name="rendererType">Renderer type</param>
        /// <param name="mainTexture">Main texture</param>
        /// <param name="shadowTexture">Shadow texture</param>
        /// <param name="tintColor">Tint color</param>
        /// <param name="blendFlag">Blend flag</param>
        /// <returns>Materials</returns>
        public Material[] CreateStandardMaterials(
            RendererType rendererType,
            (string name, ITexture2D texture) mainTexture,
            (string name, ITexture2D texture) shadowTexture,
            Color tintColor,
            GameBoxBlendFlag blendFlag);

        /// <summary>
        /// Create material for water surface
        /// </summary>
        /// <param name="mainTexture">Main texture</param>
        /// <param name="shadowTexture">Shadow texture</param>
        /// <param name="opacity">Opacity</param>
        /// <param name="blendFlag">Blend flag</param>
        /// <returns>Material</returns>
        public Material CreateWaterMaterial(
            (string name, ITexture2D texture) mainTexture,
            (string name, ITexture2D texture) shadowTexture,
            float opacity,
            GameBoxBlendFlag blendFlag);

        /// <summary>
        /// Update existing material with new texture.
        /// </summary>
        /// <param name="material">Material</param>
        /// <param name="newMainTexture">New main texture</param>
        /// <param name="blendFlag">Blend flag</param>
        public void UpdateMaterial(Material material,
            ITexture2D newMainTexture,
            GameBoxBlendFlag blendFlag);

        /// <summary>
        /// Allocate a pool of materials.
        /// </summary>
        public void AllocateMaterialPool();

        /// <summary>
        /// Deallocate the material pool.
        /// </summary>
        public void DeallocateMaterialPool();

        /// <summary>
        /// Return materials to the pool.
        /// </summary>
        /// <param name="materials">Materials to return</param>
        public void ReturnToPool(Material[] materials);
    }
}