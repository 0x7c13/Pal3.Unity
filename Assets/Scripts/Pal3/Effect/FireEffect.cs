// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Effect
{
    using System;
    using Core.DataLoader;
    using Core.Renderer;
    using Data;
    using MetaData;
    using Renderer;
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

            if (!string.IsNullOrEmpty(info.ModelPath))
            {
                (Core.DataReader.Pol.PolFile PolFile, ITextureResourceProvider TextureProvider) poly = resourceProvider.GetPol(info.ModelPath);
                _sceneObjectRenderer = gameObject.AddComponent<PolyModelRenderer>();
                _sceneObjectRenderer.Render(poly.PolFile,
                    resourceProvider.GetMaterialFactory(),
                    poly.TextureProvider,
                    Color.white);
            }

            if (!string.IsNullOrEmpty(info.TexturePathFormat))
            {
                _effectTextures = resourceProvider.GetEffectTextures(
                    GraphicsEffect.Fire,info.TexturePathFormat);

                var sprites = new Sprite[_effectTextures.Length];
                for (var i = 0; i < _effectTextures.Length; i++)
                {
                    Texture2D texture = _effectTextures[i].texture;
                    var sprite = Sprite.Create(texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0f));
                    sprites[i] = sprite;
                }
                
                Vector3 parentPosition = transform.position;

                var yPosition = parentPosition.y;
                if (_sceneObjectRenderer != null)
                {
                    yPosition = _sceneObjectRenderer.GetRendererBounds().max.y;
                }

                EffectGameObject = new GameObject($"Effect_{GraphicsEffect.Fire.ToString()}_{FireEffectType.ToString()}");
                EffectGameObject.transform.localScale =
                    new Vector3(info.Size, info.Size, info.Size);
                EffectGameObject.transform.SetParent(gameObject.transform);
                EffectGameObject.transform.position = new Vector3(parentPosition.x,
                    yPosition,
                    parentPosition.z);
                
                _billboardRenderer = EffectGameObject.AddComponent<AnimatedBillboardRenderer>();
                _spriteMaterial = _effectTextures[0].hasAlphaChannel
                    ? null
                    : resourceProvider.GetMaterialFactory().CreateOpaqueSpriteMaterial();
                
                StartCoroutine(_billboardRenderer.PlaySpriteAnimation(sprites,
                    EffectConstants.AnimatedFireEffectFrameRate,
                    -1,
                    _spriteMaterial));
            }
        }
        
        private void OnDisable()
        {
            if (_sceneObjectRenderer != null)
            {
                Destroy(_sceneObjectRenderer);
            }

            if (_billboardRenderer != null)
            {
                Destroy(_billboardRenderer);
            }

            if (EffectGameObject != null)
            {
                Destroy(EffectGameObject);
            }

            if (_spriteMaterial != null)
            {
                Destroy(_spriteMaterial);
            }
        }
    }
}