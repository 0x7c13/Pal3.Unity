// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Renderer
{
    using Core.Abstraction;
    using Core.Implementation;
    using Extensions;
    using UnityEngine;

    public class SkyBoxRenderer : GameEntityScript
    {
        private Skybox _skybox;

        private static readonly int RightTexturePropertyID = Shader.PropertyToID("_RightTex");
        private static readonly int BackTexturePropertyID = Shader.PropertyToID("_BackTex");
        private static readonly int LeftTexturePropertyID = Shader.PropertyToID("_LeftTex");
        private static readonly int FrontTexturePropertyID = Shader.PropertyToID("_FrontTex");
        private static readonly int UpTexturePropertyID = Shader.PropertyToID("_UpTex");
        private static readonly int DownTexturePropertyID = Shader.PropertyToID("_DownTex");

        protected override void OnDisableGameEntity()
        {
            if (_skybox != null)
            {
                _skybox.material.Destroy();
                _skybox.Destroy();
                _skybox = null;
            }
        }

        public void Render(IGameEntity cameraEntity,
            ITexture2D rightTex,
            ITexture2D backTex,
            ITexture2D leftTex,
            ITexture2D frontTex,
            ITexture2D upTex,
            ITexture2D downTex)
        {
            Material material = CreateSkyboxMaterial(rightTex, backTex, leftTex, frontTex, upTex, downTex);
            _skybox = cameraEntity.AddComponent<Skybox>();
            _skybox.material = material;
        }

        private static Material CreateSkyboxMaterial(ITexture2D rightTex,
            ITexture2D backTex,
            ITexture2D leftTex,
            ITexture2D frontTex,
            ITexture2D upTex,
            ITexture2D downTex)
        {
            var material = new Material(Shader.Find("Skybox/6 Sided"));
            material.SetTexture(RightTexturePropertyID, rightTex.NativeObject as Texture2D);
            material.SetTexture(BackTexturePropertyID, backTex.NativeObject as Texture2D);
            material.SetTexture(LeftTexturePropertyID, leftTex.NativeObject as Texture2D);
            material.SetTexture(FrontTexturePropertyID, frontTex.NativeObject as Texture2D);
            material.SetTexture(UpTexturePropertyID, upTex.NativeObject as Texture2D);
            material.SetTexture(DownTexturePropertyID, downTex.NativeObject as Texture2D);
            return material;
        }
    }
}