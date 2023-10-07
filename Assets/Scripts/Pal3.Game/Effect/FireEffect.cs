// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Effect
{
    using System;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Cpk;
    using Core.DataReader.Pol;
    using Core.Utilities;
    using Data;
    using Engine.Abstraction;
    using Engine.DataLoader;
    using Engine.Extensions;
    using Engine.Renderer;
    using Rendering.Material;
    using Rendering.Renderer;
    using UnityEngine;

    public sealed class FireEffect : GameEntityScript, IEffect
    {
        public IGameEntity EffectGameEntity { get; private set; }
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
                    CoreUtility.GetDirectoryName(info.ModelPath, CpkConstants.DirectorySeparatorChar));
                _sceneObjectRenderer = GameEntity.AddComponent<PolyModelRenderer>();
                _sceneObjectRenderer.Render(polFile,
                    textureProvider,
                    materialFactory,
                    isStaticObject: false);
            }

            if (!string.IsNullOrEmpty(info.TexturePathFormat))
            {
                _effectTextures = resourceProvider.GetEffectTextures(
                    GraphicsEffectType.Fire, info.TexturePathFormat);

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

                EffectGameEntity = new GameEntity($"Effect_{GraphicsEffectType.Fire.ToString()}_{FireEffectType.ToString()}");
                EffectGameEntity.SetParent(GameEntity, worldPositionStays: false);
                EffectGameEntity.Transform.LocalScale = new Vector3(info.Size, info.Size, info.Size);
                EffectGameEntity.Transform.LocalPosition = new Vector3(0f, yPosition, 0f);

                _billboardRenderer = EffectGameEntity.AddComponent<AnimatedBillboardRenderer>();
                _spriteMaterial = _effectTextures[0].hasAlphaChannel
                    ? null
                    : materialFactory.CreateOpaqueSpriteMaterial();
                _billboardRenderer.Init(sprites, EffectConstants.AnimatedFireEffectFrameRate, _spriteMaterial);
                _billboardRenderer.StartAnimation(-1);
            }
        }

        protected override void OnDestroyGameEntity()
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

            if (EffectGameEntity != null)
            {
                EffectGameEntity.Destroy();
                EffectGameEntity = null;
            }

            if (_spriteMaterial != null)
            {
                _spriteMaterial.Destroy();
                _spriteMaterial = null;
            }
        }
    }
}