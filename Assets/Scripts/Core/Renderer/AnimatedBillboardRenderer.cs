// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Renderer
{
    using System.Collections;
    using UnityEngine;

    public class AnimatedBillboardRenderer : MonoBehaviour
    {
        private StaticBillboardRenderer _billboardRenderer;
        private SpriteRenderer _spriteRenderer;
        private bool _isPlaying;

        public void OnEnable()
        {
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            _billboardRenderer = gameObject.AddComponent<StaticBillboardRenderer>();
        }

        private void OnDisable()
        {
            _isPlaying = false;
            Destroy(_billboardRenderer);
            Destroy(_spriteRenderer);
        }

        public IEnumerator PlaySpriteAnimation(Sprite[] sprites,
            float fps,
            int loopCount,
            Material material = default)
        {
            _isPlaying = true;

            if (material != default)
            {
                _spriteRenderer.material = material;
            }

            if (loopCount == -1)
            {
                while (_isPlaying)
                {
                    yield return PlaySpriteAnimationInternal(sprites, fps);
                }
            }
            else
            {
                while (--loopCount >= 0 && _isPlaying)
                {
                    yield return PlaySpriteAnimationInternal(sprites, fps);
                }
            }
        }

        private IEnumerator PlaySpriteAnimationInternal(Sprite[] sprites, float fps)
        {
            foreach (Sprite sprite in sprites)
            {
                if (!_isPlaying) yield break;
                _spriteRenderer.sprite = sprite;
                yield return new WaitForSeconds(1 / fps);
            }
        }
    }
}