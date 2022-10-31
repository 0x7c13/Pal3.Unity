// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Effect
{
    using System;
    using Data;
    using System.Collections;
    using UnityEngine;

    public class RotatingSpriteEffect : MonoBehaviour, IDisposable
    {
        private GameObject _root;
        private SpriteRenderer _spriteRenderer;
        private Material _material;
        private Coroutine _animation;

        private float _rotationSpeed;

        public void Init(GameResourceProvider resourceProvider,
            string textureName,
            Vector3 scale,
            float rotationSpeed)
        {
            _rotationSpeed = rotationSpeed;
            
            Texture2D texture = resourceProvider.GetEffectTexture(textureName, out var hasAlphaChannel);

            _root = new GameObject($"RotatingSpriteEffect_{textureName}");
            _root.transform.SetParent(transform, false);
            
            _spriteRenderer = _root.AddComponent<SpriteRenderer>();
            
            _spriteRenderer.sprite = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));

            if (!hasAlphaChannel)
            {
                _material = resourceProvider.GetMaterialFactory().CreateOpaqueSpriteMaterial(texture);
                _spriteRenderer.sharedMaterial = _material;                
            }

            Quaternion parentRotation = gameObject.transform.rotation;
            _root.transform.localRotation = Quaternion.Euler(parentRotation.x + 90f, 0f, 0f);
            _root.transform.localScale = scale;

            _animation = StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            while (isActiveAndEnabled)
            {
                var rotationDelta = _rotationSpeed * Time.deltaTime;
                _root.transform.localRotation *= Quaternion.Euler(0f, 0f, -rotationDelta);
                yield return null;
            }
        }

        private void OnDisable()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_animation != null)
            {
                StopCoroutine(_animation);
            }

            if (_material != null)
            {
                Destroy(_material);
            }

            if (_spriteRenderer != null)
            {
                Destroy(_spriteRenderer);
            }

            if (_root != null)
            {
                Destroy(_root);
            }
        }
    }
}