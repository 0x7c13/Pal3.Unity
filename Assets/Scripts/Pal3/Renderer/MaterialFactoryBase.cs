// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Renderer
{
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class MaterialFactoryBase
    {
        private const string SPRITE_SHADER_PATH = "Pal3/Sprite";
        
        // Sprite material uniforms
        private static readonly int SpriteMainTexPropertyId = Shader.PropertyToID("_MainTex");
        
        private readonly Dictionary<string, Shader> _shaders = new ();
        
        public Material CreateSpriteMaterial()
        {
            return new Material(GetShader(SPRITE_SHADER_PATH));
        }
        
        public Material CreateSpriteMaterial(Texture2D texture)
        {
            var material = new Material(GetShader(SPRITE_SHADER_PATH));
            material.SetTexture(SpriteMainTexPropertyId, texture);
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