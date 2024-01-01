// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
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
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
    using Engine.Extensions;
    using Engine.Renderer;
    using Rendering.Material;
    using Rendering.Renderer;

    using Vector3 = UnityEngine.Vector3;

    public sealed class FireEffect : GameEntityScript, IEffect
    {
        public IGameEntity EffectGameEntity { get; private set; }
        public FireEffectType FireEffectType { get; private set; }

        private (ITexture2D texture, bool hasAlphaChannel)[] _effectTextures = Array.Empty<(ITexture2D texture, bool hasAlphaChannel)>();
        private AnimatedBillboardRenderer _billboardRenderer;
        private PolyModelRenderer _sceneObjectRenderer;
        private IMaterial _spriteMaterial;

        protected override void OnDisableGameEntity()
        {
            Dispose();
        }

        public void Init(GameResourceProvider resourceProvider, uint effectParameter)
        {
            FireEffectType = (FireEffectType)effectParameter;
            (string TexturePathFormat, string ModelPath, float Size, float _) info = EffectConstants.FireEffectInfo[FireEffectType];
            IMaterialManager materialManager = resourceProvider.GetMaterialManager();

            if (!string.IsNullOrEmpty(info.ModelPath))
            {
                PolFile polFile = resourceProvider.GetGameResourceFile<PolFile>(info.ModelPath);
                ITextureResourceProvider textureProvider = resourceProvider.CreateTextureResourceProvider(
                    CoreUtility.GetDirectoryName(info.ModelPath, CpkConstants.DirectorySeparatorChar));
                _sceneObjectRenderer = GameEntity.AddComponent<PolyModelRenderer>();
                _sceneObjectRenderer.Render(polFile,
                    textureProvider,
                    materialManager,
                    isStaticObject: false);
            }

            if (!string.IsNullOrEmpty(info.TexturePathFormat))
            {
                _effectTextures = resourceProvider.GetEffectTextures(
                    GraphicsEffectType.Fire, info.TexturePathFormat);

                var sprites = new ISprite[_effectTextures.Length];
                for (var i = 0; i < _effectTextures.Length; i++)
                {
                    ITexture2D texture = _effectTextures[i].texture;
                    ISprite sprite = texture.CreateSprite(0f, 0f,
                        texture.Width, texture.Height,
                        0.5f, 0f);
                    sprites[i] = sprite;
                }

                var yPosition = 0f;
                if (_sceneObjectRenderer != null)
                {
                    yPosition = _sceneObjectRenderer.GetMeshBounds().max.y;
                }

                EffectGameEntity = GameEntityFactory.Create($"Effect_{GraphicsEffectType.Fire.ToString()}_{FireEffectType.ToString()}",
                    GameEntity, worldPositionStays: false);
                EffectGameEntity.Transform.LocalScale = new Vector3(info.Size, info.Size, info.Size);
                EffectGameEntity.Transform.LocalPosition = new Vector3(0f, yPosition, 0f);

                _billboardRenderer = EffectGameEntity.AddComponent<AnimatedBillboardRenderer>();
                _spriteMaterial = _effectTextures[0].hasAlphaChannel
                    ? null
                    : materialManager.CreateOpaqueSpriteMaterial();
                _billboardRenderer.Init(sprites, EffectConstants.AnimatedFireEffectFrameRate, _spriteMaterial);
                _billboardRenderer.StartAnimation(-1);
            }
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