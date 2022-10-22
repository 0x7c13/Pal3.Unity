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
        private Texture2D[] _effectTextures = Array.Empty<Texture2D>();
        private AnimatedBillboardRenderer _billboardRenderer;
        private PolyModelRenderer _sceneObjectRenderer;
        private GameObject _effectGameObject;

        public void Init(GameResourceProvider resourceProvider, uint effectParameter)
        {
            var fireEffectType = (FireEffectType)effectParameter;
            (string TexturePathFormat, string ModelPath, float Size) info = EffectConstants.FireEffectInfo[fireEffectType];

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
                    Texture2D texture = _effectTextures[i];
                    var sprite = Sprite.Create(texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0f));
                    sprites[i] = sprite;
                }

                var fps = EffectConstants.EffectAnimationInfo[GraphicsEffect.Fire].Fps;

                Vector3 parentPosition = transform.position;

                var yPosition = parentPosition.y;
                if (_sceneObjectRenderer != null)
                {
                    yPosition = _sceneObjectRenderer.GetRendererBounds().max.y;
                }

                _effectGameObject = new GameObject($"Effect_{GraphicsEffect.Fire.ToString()}");
                _effectGameObject.transform.localScale =
                    new Vector3(info.Size, info.Size, info.Size);
                _effectGameObject.transform.SetParent(gameObject.transform);
                _effectGameObject.transform.position = new Vector3(parentPosition.x,
                    yPosition,
                    parentPosition.z);
                _billboardRenderer = _effectGameObject.AddComponent<AnimatedBillboardRenderer>();
                StartCoroutine(_billboardRenderer.PlaySpriteAnimation(sprites,
                    fps,
                    -1,
                    resourceProvider.GetMaterialFactory().CreateSpriteMaterial()));
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

            if (_effectGameObject != null)
            {
                Destroy(_effectGameObject);
            }
        }
    }
}