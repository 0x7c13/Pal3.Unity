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

        public void Render(Texture2D[] textures)
        {
            var mainCamera = GetComponent<Camera>();
            if (mainCamera == null)
            {
                throw new Exception("SkyBoxRenderer needs to be attached to a camera game object.");
            }
            Material material = CreateSkyboxMaterial(textures);
            _skybox = mainCamera.gameObject.AddComponent<Skybox>();
            _skybox.material = material;
        }

        private void OnDisable()
        {
            if (_skybox != null) Destroy(_skybox);
        }

        private static Material CreateSkyboxMaterial(Texture2D[] textures)
        {
            var material = new Material(Shader.Find("Skybox/6 Sided"));
            material.SetTexture(RightTexturePropertyID, textures[0]);
            material.SetTexture(BackTexturePropertyID, textures[1]);
            material.SetTexture(LeftTexturePropertyID, textures[2]);
            material.SetTexture(FrontTexturePropertyID, textures[3]);
            material.SetTexture(UpTexturePropertyID, textures[4]);
            material.SetTexture(DownTexturePropertyID, textures[5]);
            return material;
        }
    }
}