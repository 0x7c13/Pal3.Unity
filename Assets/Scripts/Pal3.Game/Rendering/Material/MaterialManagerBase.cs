// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Material
{
    using Engine.Core.Abstraction;
    using Engine.Services;

    public abstract class MaterialManagerBase
    {
        private const string SKYBOX_SHADER_PATH = "Skybox/6 Sided";
        private const string SPRITE_SHADER_PATH = "Pal3/Sprite";

        private static readonly int RightTexturePropertyID = ShaderUtility.GetPropertyIdByName("_RightTex");
        private static readonly int BackTexturePropertyID = ShaderUtility.GetPropertyIdByName("_BackTex");
        private static readonly int LeftTexturePropertyID = ShaderUtility.GetPropertyIdByName("_LeftTex");
        private static readonly int FrontTexturePropertyID = ShaderUtility.GetPropertyIdByName("_FrontTex");
        private static readonly int UpTexturePropertyID = ShaderUtility.GetPropertyIdByName("_UpTex");
        private static readonly int DownTexturePropertyID = ShaderUtility.GetPropertyIdByName("_DownTex");

        internal IMaterialFactory MaterialFactory { get; }

        protected MaterialManagerBase(IMaterialFactory materialFactory)
        {
            MaterialFactory = materialFactory;
        }

        public IMaterial CreateOpaqueSpriteMaterial()
        {
            return MaterialFactory.CreateMaterial(SPRITE_SHADER_PATH);
        }

        public IMaterial CreateOpaqueSpriteMaterial(ITexture2D texture)
        {
            var material = MaterialFactory.CreateMaterial(SPRITE_SHADER_PATH);
            material.SetMainTexture(texture);
            return material;
        }

        public IMaterial CreateSkyboxMaterial(
            ITexture2D rightTex,
            ITexture2D backTex,
            ITexture2D leftTex,
            ITexture2D frontTex,
            ITexture2D upTex,
            ITexture2D downTex)
        {
            IMaterial material = MaterialFactory.CreateMaterial(SKYBOX_SHADER_PATH);
            material.SetTexture(RightTexturePropertyID, rightTex);
            material.SetTexture(BackTexturePropertyID, backTex);
            material.SetTexture(LeftTexturePropertyID, leftTex);
            material.SetTexture(FrontTexturePropertyID, frontTex);
            material.SetTexture(UpTexturePropertyID, upTex);
            material.SetTexture(DownTexturePropertyID, downTex);
            return material;
        }


        public void ReturnToPool(IMaterial[] materials)
        {
            if (materials == null) return;

            foreach (IMaterial material in materials)
            {
                ReturnToPool(material);
            }
        }

        protected abstract void ReturnToPool(IMaterial material);
    }
}