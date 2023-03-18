// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Renderer
{
    using System;
    using UnityEngine;

    public class SkyBoxRenderer : MonoBehaviour
    {
        private Skybox _skybox;

        private static readonly int RightTexturePropertyID = Shader.PropertyToID("_RightTex");
        private static readonly int BackTexturePropertyID = Shader.PropertyToID("_BackTex");
        private static readonly int LeftTexturePropertyID = Shader.PropertyToID("_LeftTex");
        private static readonly int FrontTexturePropertyID = Shader.PropertyToID("_FrontTex");
        private static readonly int UpTexturePropertyID = Shader.PropertyToID("_UpTex");
        private static readonly int DownTexturePropertyID = Shader.PropertyToID("_DownTex");

        public void Render(Camera targetCamera,
            Texture2D rightTex,
            Texture2D backTex,
            Texture2D leftTex,
            Texture2D frontTex,
            Texture2D upTex,
            Texture2D downTex)
        {
            Material material = CreateSkyboxMaterial(rightTex, backTex, leftTex, frontTex, upTex, downTex);
            _skybox = targetCamera.gameObject.AddComponent<Skybox>();
            _skybox.material = material;
        }

        private void OnDisable()
        {
            if (_skybox != null) Destroy(_skybox);
        }

        private static Material CreateSkyboxMaterial(Texture2D rightTex,
            Texture2D backTex,
            Texture2D leftTex,
            Texture2D frontTex,
            Texture2D upTex,
            Texture2D downTex)
        {
            var material = new Material(Shader.Find("Skybox/6 Sided"));
            material.SetTexture(RightTexturePropertyID, rightTex);
            material.SetTexture(BackTexturePropertyID, backTex);
            material.SetTexture(LeftTexturePropertyID, leftTex);
            material.SetTexture(FrontTexturePropertyID, frontTex);
            material.SetTexture(UpTexturePropertyID, upTex);
            material.SetTexture(DownTexturePropertyID, downTex);
            return material;
        }
    }
}