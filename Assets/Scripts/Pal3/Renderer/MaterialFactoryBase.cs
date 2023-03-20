// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Renderer
{
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class MaterialFactoryBase
    {
        private const string SPRITE_SHADER_PATH = "Pal3/Sprite";
        private const string WATER_SHADER_PATH = "Pal3/Water";

        // Water material uniforms
        private static readonly int WaterMainTexPropId = Shader.PropertyToID("_MainTex");
        private static readonly int WaterShadowTexPropId = Shader.PropertyToID("_ShadowTex");
        private static readonly int WaterAlphaPropId = Shader.PropertyToID("_Alpha");
        private static readonly int WaterHasShadowTexPropId = Shader.PropertyToID("_HasShadowTex");

        // Sprite material uniforms
        private static readonly int SpriteMainTexPropertyId = Shader.PropertyToID("_MainTex");

        private readonly Dictionary<string, Shader> _shaders = new ();

        public Material CreateOpaqueSpriteMaterial()
        {
            return new Material(GetShader(SPRITE_SHADER_PATH));
        }

        public Material CreateOpaqueSpriteMaterial(Texture2D texture)
        {
            var material = new Material(GetShader(SPRITE_SHADER_PATH));
            material.SetTexture(SpriteMainTexPropertyId, texture);
            return material;
        }

        public Material CreateWaterMaterial(
            (string name, Texture2D texture) mainTexture,
            (string name, Texture2D texture) shadowTexture,
            float alpha)
        {
            var material = new Material(GetShader(WATER_SHADER_PATH));
            material.SetTexture(WaterMainTexPropId,mainTexture.texture);
            material.SetFloat(WaterAlphaPropId, alpha);
            if (shadowTexture.texture != null)
            {
                material.SetFloat(WaterHasShadowTexPropId, 1.0f);
                material.SetTexture(WaterShadowTexPropId, shadowTexture.texture);
            }
            else
            {
                material.SetFloat(WaterHasShadowTexPropId, 0.5f);
            }
            return material;
        }

        internal Shader GetShader(string shaderName)
        {
            if (_shaders.ContainsKey(shaderName)) return _shaders[shaderName];

            var shader = Shader.Find(shaderName);

            if (shader != null)
            {
                _shaders[shaderName] = shader;
            }

            return shader;
        }
    }
}