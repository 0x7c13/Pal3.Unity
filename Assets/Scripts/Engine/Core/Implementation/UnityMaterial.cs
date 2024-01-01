// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Core.Implementation
{
    using Abstraction;
    using Extensions;
    using UnityEngine;

    public sealed class UnityMaterial : IMaterial
    {
        public object NativeObject => _material;

        public bool IsNativeObjectDisposed => _material == null;

        private Material _material;

        public UnityMaterial(Shader shader)
        {
            _material = new Material(shader);
        }

        public UnityMaterial(Material material, bool isClone)
        {
            _material = isClone ? new Material(material) : material;
        }

        public string ShaderName => _material.shader.name;

        public float GetFloat(int propertyId) => _material.GetFloat(propertyId);

        public int GetInt(int propertyId) => _material.GetInt(propertyId);

        public void SetInt(int propertyId, int value) => _material.SetInt(propertyId, value);

        public void SetFloat(int propertyId, float value) => _material.SetFloat(propertyId, value);

        public void SetColor(int propertyId, Pal3.Core.Primitives.Color value) => _material.SetColor(propertyId, value.ToUnityColor());

        public void SetMainTexture(ITexture2D texture)
        {
            if (texture is UnityTexture2D unityTexture)
            {
                _material.mainTexture = unityTexture.NativeObject as Texture2D;
            }
            else
            {
                _material.mainTexture = null;
            }
        }

        public void SetTexture(int propertyId, ITexture2D texture)
        {
            if (texture is UnityTexture2D unityTexture)
            {
                _material.SetTexture(propertyId, unityTexture.NativeObject as Texture2D);
            }
            else
            {
                _material.SetTexture(propertyId, null);
            }
        }

        public void SetMainTextureScale(float x, float y) => _material.mainTextureScale = new Vector2(x, y);

        public static implicit operator Material(UnityMaterial m) => m.NativeObject as Material;

        public void Destroy()
        {
            if (!IsNativeObjectDisposed)
            {
                _material.Destroy();
            }

            _material = null;
        }
    }
}