// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Material
{
    using System;
    using Core.Primitives;
    using Engine.Core.Abstraction;
    using Color = Core.Primitives.Color;

    public interface IMaterialManager : IDisposable
    {
        public bool UsesToonRendering { get; }

        /// <summary>
        /// Create a material for effect sprite without alpha channel.
        /// </summary>
        /// <returns>Material</returns>
        public IMaterial CreateOpaqueSpriteMaterial();

        /// <summary>
        /// Create a material for effect sprite without alpha channel.
        /// </summary>
        /// <param name="texture">Texture</param>
        /// <returns>Material</returns>
        public IMaterial CreateOpaqueSpriteMaterial(ITexture2D texture);

        /// <summary>
        /// Create standard materials.
        /// </summary>
        /// <param name="rendererType">Renderer type</param>
        /// <param name="mainTexture">Main texture</param>
        /// <param name="bakedLightmap">Baked lightmap</param>
        /// <param name="tintColor">Tint color</param>
        /// <param name="blendFlag">Blend flag</param>
        /// <returns>Materials</returns>
        public IMaterial[] CreateStandardMaterials(
            RendererType rendererType,
            (string name, ITexture2D texture) mainTexture,
            (string name, ITexture2D texture) bakedLightmap,
            Color tintColor,
            GameBoxBlendFlag blendFlag);

        /// <summary>
        /// Create material for water surface
        /// </summary>
        /// <param name="mainTexture">Main texture</param>
        /// <param name="bakedLightmap">Baked lightmap</param>
        /// <param name="opacity">Opacity</param>
        /// <returns>Material</returns>
        public IMaterial CreateWaterMaterial(
            (string name, ITexture2D texture) mainTexture,
            (string name, ITexture2D texture) bakedLightmap,
            float opacity);

        /// <summary>
        /// Create material for sky box
        /// </summary>
        /// <param name="rightTex">Right side texture</param>
        /// <param name="backTex">Back side texture</param>
        /// <param name="leftTex">Left side texture</param>
        /// <param name="frontTex">Front side texture</param>
        /// <param name="upTex">Up side texture</param>
        /// <param name="downTex">Down side texture</param>
        /// <returns></returns>
        public IMaterial CreateSkyboxMaterial(
            ITexture2D rightTex,
            ITexture2D backTex,
            ITexture2D leftTex,
            ITexture2D frontTex,
            ITexture2D upTex,
            ITexture2D downTex);

        /// <summary>
        /// Return materials to the pool.
        /// </summary>
        /// <param name="materials">Materials to return</param>
        public void ReturnToPool(IMaterial[] materials);
    }
}