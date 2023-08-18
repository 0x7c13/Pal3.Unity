// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Rendering.Material
{
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class MaterialFactoryBase
    {
        private const string SPRITE_SHADER_PATH = "Pal3/Sprite";

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

        internal Shader GetShader(string shaderName)
        {
            if (_shaders.TryGetValue(shaderName, out Shader shaderInCache)) return shaderInCache;

            var shader = Shader.Find(shaderName);

            if (shader != null)
            {
                _shaders[shaderName] = shader;
            }

            return shader;
        }

        public void ReturnToPool(Material[] materials)
        {
            if (materials == null) return;

            foreach (Material material in materials)
            {
                ReturnToPool(material);
            }
        }

        protected virtual void ReturnToPool(Material material) { }
    }
}