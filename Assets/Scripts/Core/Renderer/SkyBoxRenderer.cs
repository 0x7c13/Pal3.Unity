// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Renderer
{
    using System;
    using UnityEngine;

    public class SkyBoxRenderer : MonoBehaviour
    {
        private Skybox _skybox;

        public void Render(Texture2D[] textures)
        {
            var mainCamera = GetComponent<Camera>();
            if (mainCamera == null)
            {
                throw new Exception("SkyBoxRenderer needs to be attached to a camera game object.");
            }
            var material = CreateSkyboxMaterial(textures);
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
            material.SetTexture("_RightTex", textures[0]);
            material.SetTexture("_BackTex", textures[1]);
            material.SetTexture("_LeftTex", textures[2]);
            material.SetTexture("_FrontTex", textures[3]);
            material.SetTexture("_UpTex", textures[4]);
            material.SetTexture("_DownTex", textures[5]);
            return material;
        }
    }
}