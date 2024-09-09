// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Renderer
{
    using System.Collections;
    using System.Threading;
    using Core.Abstraction;
    using Core.Implementation;
    using Coroutine;
    using Extensions;
    using UnityEngine;

    public class AnimatedBillboardRenderer : GameEntityScript
    {
        private ISprite[] _sprites;
        private object _spriteAnimationFrameWaiter;
        private bool _initialized;

        private StaticBillboardRenderer _billboardRenderer;
        private SpriteRenderer _spriteRenderer;
        private CancellationTokenSource _animationCts = new ();

        protected override void OnEnableGameEntity()
        {
            _spriteRenderer = GameEntity.AddComponent<SpriteRenderer>();
            _billboardRenderer = GameEntity.AddComponent<StaticBillboardRenderer>();
        }

        protected override void OnDisableGameEntity()
        {
            _animationCts.Cancel();
            _animationCts = null;

            _spriteRenderer.Destroy();
            _spriteRenderer = null;

            _billboardRenderer.Destroy();
            _billboardRenderer = null;
        }

        public void Init(ISprite[] sprites,
            float fps,
            IMaterial material = default)
        {
            _sprites = sprites;

            if (material != default)
            {
                _spriteRenderer.material = material.NativeObject as Material;
            }

            _spriteAnimationFrameWaiter = CoroutineYieldInstruction.WaitForSeconds(1 / fps);

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
            CancellationToken cancellationToken = _animationCts.Token;

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
            foreach (ISprite sprite in _sprites)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }
                _spriteRenderer.sprite = sprite.NativeObject as Sprite;
                yield return _spriteAnimationFrameWaiter;
            }
        }
    }
}