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
                
                #if RTX_ON
                AddLightSource(_effectGameObject.transform, 0.2f);
                #endif
            }
        }

        private void AddLightSource(Transform parent, float yOffset)
        {
            // Add a point light to the fire fx
            var lightSource = new GameObject($"LightSource_Point");
            lightSource.transform.SetParent(parent, false);
            lightSource.transform.localPosition = new Vector3(0f, yOffset, 0f);
            
            var lightComponent = lightSource.AddComponent<Light>();
            lightComponent.color = new Color(220f / 255f, 145f / 255f, 105f / 255f);
            lightComponent.type = LightType.Point;
            lightComponent.intensity = 1f;
            lightComponent.range = 50f;
            lightComponent.shadows = LightShadows.Soft;
            lightComponent.shadowNearPlane = 0.3f;
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