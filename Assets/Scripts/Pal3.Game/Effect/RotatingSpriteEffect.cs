// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Effect
{
    using System;
    using Data;
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
    using Engine.Extensions;
    using UnityEngine;

    public sealed class RotatingSpriteEffect : TickableGameEntityScript, IDisposable
    {
        private IGameEntity _root;
        private SpriteRenderer _spriteRenderer;
        private IMaterial _material;

        private float _rotationSpeed;

        protected override void OnDisableGameEntity()
        {
            Dispose();
        }

        public void Init(GameResourceProvider resourceProvider,
            string textureName,
            Vector3 scale,
            float rotationSpeed)
        {
            _rotationSpeed = rotationSpeed;

            ITexture2D texture = resourceProvider.GetEffectTexture(textureName, out var hasAlphaChannel);

            _root = GameEntityFactory.Create($"RotatingSpriteEffect_{textureName}",
                GameEntity, worldPositionStays: false);

            _spriteRenderer = _root.AddComponent<SpriteRenderer>();

            _spriteRenderer.sprite = texture.CreateSprite(0, 0,
                texture.Width, texture.Height,
                0.5f, 0.5f).NativeObject as Sprite;

            if (!hasAlphaChannel)
            {
                _material = resourceProvider.GetMaterialManager()
                    .CreateOpaqueSpriteMaterial(texture);
                _spriteRenderer.sharedMaterial = _material.NativeObject as Material;
            }

            Quaternion parentRotation = GameEntity.Transform.Rotation;
            _root.Transform.LocalRotation = Quaternion.Euler(parentRotation.x + 90f, 0f, 0f);
            _root.Transform.LocalScale = scale;
        }

        protected override void OnUpdateGameEntity(float deltaTime)
        {
            if (_root is { IsNativeObjectDisposed: false })
            {
                var rotationDelta = _rotationSpeed * deltaTime;
                _root.Transform.LocalRotation *= Quaternion.Euler(0f, 0f, -rotationDelta);
            }
        }

        public void Dispose()
        {
            if (_material != null)
            {
                _material.Destroy();
                _material = null;
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.Destroy();
                _spriteRenderer = null;
            }

            if (_root != null)
            {
                _root.Destroy();
                _root = null;
            }
        }
    }
}