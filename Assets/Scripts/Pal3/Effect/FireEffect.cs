// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Effect
{
    using System;
    using Core.DataLoader;
    using Core.DataReader.Cpk;
    using Core.DataReader.Pol;
    using Core.Extensions;
    using Core.Renderer;
    using Core.Utils;
    using Data;
    using MetaData;
    using Rendering.Material;
    using Rendering.Renderer;
    using UnityEngine;

    public class FireEffect : MonoBehaviour, IEffect
    {
        public GameObject EffectGameObject { get; private set; }
        public FireEffectType FireEffectType { get; private set; }

        private (Texture2D texture, bool hasAlphaChannel)[] _effectTextures = Array.Empty<(Texture2D texture, bool hasAlphaChannel)>();
        private AnimatedBillboardRenderer _billboardRenderer;
        private PolyModelRenderer _sceneObjectRenderer;
        private Material _spriteMaterial;

        public void Init(GameResourceProvider resourceProvider, uint effectParameter)
        {
            FireEffectType = (FireEffectType)effectParameter;
            (string TexturePathFormat, string ModelPath, float Size, float _) info = EffectConstants.FireEffectInfo[FireEffectType];
            IMaterialFactory materialFactory = resourceProvider.GetMaterialFactory();

            if (!string.IsNullOrEmpty(info.ModelPath))
            {
                PolFile polFile = resourceProvider.GetGameResourceFile<PolFile>(info.ModelPath);
                ITextureResourceProvider textureProvider = resourceProvider.CreateTextureResourceProvider(
                    Utility.GetDirectoryName(info.ModelPath, CpkConstants.DirectorySeparatorChar));
                _sceneObjectRenderer = gameObject.AddComponent<PolyModelRenderer>();
                _sceneObjectRenderer.Render(polFile,
                    textureProvider,
                    materialFactory,
                    isStaticObject: false);
            }

            if (!string.IsNullOrEmpty(info.TexturePathFormat))
            {
                _effectTextures = resourceProvider.GetEffectTextures(
                    GraphicsEffect.Fire, info.TexturePathFormat);

                var sprites = new Sprite[_effectTextures.Length];
                for (var i = 0; i < _effectTextures.Length; i++)
                {
                    Texture2D texture = _effectTextures[i].texture;
                    var sprite = Sprite.Create(texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0f));
                    sprites[i] = sprite;
                }

                var yPosition = 0f;
                if (_sceneObjectRenderer != null)
                {
                    yPosition = _sceneObjectRenderer.GetMeshBounds().max.y;
                }

                EffectGameObject = new GameObject($"Effect_{GraphicsEffect.Fire.ToString()}_{FireEffectType.ToString()}");
                EffectGameObject.transform.SetParent(gameObject.transform, false);
                EffectGameObject.transform.localScale = new Vector3(info.Size, info.Size, info.Size);
                EffectGameObject.transform.localPosition = new Vector3(0f, yPosition, 0f);

                _billboardRenderer = EffectGameObject.AddComponent<AnimatedBillboardRenderer>();
                _spriteMaterial = _effectTextures[0].hasAlphaChannel
                    ? null
                    : materialFactory.CreateOpaqueSpriteMaterial();
                _billboardRenderer.Init(sprites, EffectConstants.AnimatedFireEffectFrameRate, _spriteMaterial);
                _billboardRenderer.StartAnimation(-1);
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_sceneObjectRenderer != null)
            {
                _sceneObjectRenderer.Dispose();
                _sceneObjectRenderer.Destroy();
                _sceneObjectRenderer = null;
            }

            if (_billboardRenderer != null)
            {
                _billboardRenderer.Destroy();
                _billboardRenderer = null;
            }

            if (EffectGameObject != null)
            {
                EffectGameObject.Destroy();
                EffectGameObject = null;
            }

            if (_spriteMaterial != null)
            {
                _spriteMaterial.Destroy();
                _spriteMaterial = null;
            }
        }
    }
}