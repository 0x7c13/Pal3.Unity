// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Renderer
{
    using System.Collections;
    using System.Threading;
    using UnityEngine;

    public class AnimatedBillboardRenderer : MonoBehaviour
    {
        private Sprite[] _sprites;
        private WaitForSeconds _spriteAnimationFrameWaiter;
        private bool _initialized;

        private StaticBillboardRenderer _billboardRenderer;
        private SpriteRenderer _spriteRenderer;
        private CancellationTokenSource _animationCts = new ();

        public void OnEnable()
        {
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            _billboardRenderer = gameObject.AddComponent<StaticBillboardRenderer>();
        }

        private void OnDisable()
        {
            _animationCts.Cancel();
            Destroy(_billboardRenderer);
            Destroy(_spriteRenderer);
        }

        public void Init(Sprite[] sprites,
            float fps,
            Material material = default)
        {
            _sprites = sprites;

            if (material != default)
            {
                _spriteRenderer.material = material;
            }

            _spriteAnimationFrameWaiter = new WaitForSeconds(1 / fps);

            _initialized = true;
        }

        public void StartAnimation(int loopCount = -1)
        {
            StartCoroutine(PlayAnimationAsync(loopCount));
        }

        public IEnumerator PlayAnimationAsync(int loopCount)
        {
            if (!_initialized) yield break;

            _animationCts.Cancel();
            _animationCts = new CancellationTokenSource();
            var cancellationToken = _animationCts.Token;

            _spriteRenderer.enabled = true;

            if (loopCount == -1)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    yield return PlaySpriteAnimationInternalAsync(cancellationToken);
                }
            }
            else
            {
                while (--loopCount >= 0 && !cancellationToken.IsCancellationRequested)
                {
                    yield return PlaySpriteAnimationInternalAsync(cancellationToken);
                }
            }
        }

        public void StopAnimation()
        {
            if (_initialized && !_animationCts.IsCancellationRequested)
            {
                _animationCts.Cancel();
                _spriteRenderer.enabled = false;
            }
        }

        private IEnumerator PlaySpriteAnimationInternalAsync(CancellationToken cancellationToken)
        {
            foreach (Sprite sprite in _sprites)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }
                _spriteRenderer.sprite = sprite;
                yield return _spriteAnimationFrameWaiter;
            }
        }
    }
}