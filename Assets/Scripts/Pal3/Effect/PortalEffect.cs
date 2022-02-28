// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Effect
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Core.Renderer;
    using Data;
    using MetaData;
    using Renderer;
    using UnityEditor;
    using UnityEngine;

    public class PortalEffect : MonoBehaviour, IEffect
    {
        private const string PORTAL_BASE_TEXTURE_NAME = "trans.dds";
        private const float PORTAL_DEFAULT_SIZE = 1.3f;
        private const float PORTAL_ANIMATION_ROTATION_SPEED = 5f;

        private SpriteRenderer _spriteRenderer;
        private Coroutine _portalAnimation;

        public void Init(GameResourceProvider resourceProvider, uint effectModelType)
        {
            var baseTexture = resourceProvider.GetEffectTexture(PORTAL_BASE_TEXTURE_NAME);

            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            _spriteRenderer.sprite = Sprite.Create(baseTexture,
                new Rect(0, 0, baseTexture.width, baseTexture.height),
                new Vector2(0.5f, 0.5f));

            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            transform.localScale =
                new Vector3(PORTAL_DEFAULT_SIZE, PORTAL_DEFAULT_SIZE, PORTAL_DEFAULT_SIZE);

            _portalAnimation = StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            while (isActiveAndEnabled)
            {
                var rotationDelta = PORTAL_ANIMATION_ROTATION_SPEED * Time.deltaTime;
                gameObject.transform.localRotation *= Quaternion.Euler(0f, 0f, -rotationDelta);
                yield return null;
            }
        }

        private void OnDisable()
        {
            if (_portalAnimation != null)
            {
                StopCoroutine(_portalAnimation);
            }

            if (_spriteRenderer != null)
            {
                Destroy(_spriteRenderer);
            }
        }
    }
}