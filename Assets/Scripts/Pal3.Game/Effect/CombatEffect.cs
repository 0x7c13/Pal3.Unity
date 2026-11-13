// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect
{
    using System;
    using Data;
    using Engine.Core.Implementation;
    using Engine.Extensions;

    using Vector3 = UnityEngine.Vector3;

    public sealed class CombatEffect : GameEntityScript, IEffect
    {
#if PAL3
        private const string SHUSHAN_BRIDGE_TEXTURE_NAME = "q08qiao.dds";
#elif PAL3A
        private const string SHUSHAN_BRIDGE_TEXTURE_NAME = "q08qiao.tga";
#endif
        private const float SHUSHAN_BRIDGE_DEFAULT_SIZE = 3.5f;
        private const float SHUSHAN_BRIDGE_ANIMATION_ROTATION_SPEED = 15f;

#if PAL3
        private const string SHUSHAN_BRIDGE_EFFECT_TEXTURE_NAME = "q08qiao2.dds";
#elif PAL3A
        private const string SHUSHAN_BRIDGE_EFFECT_TEXTURE_NAME = "q08qiao2.tga";
#endif
        private const float SHUSHAN_BRIDGE_EFFECT_DEFAULT_SIZE = 3.5f;
        private const float SHUSHAN_BRIDGE_EFFECT_ANIMATION_ROTATION_SPEED = 40f;

        private RotatingSpriteEffect _bridgeBaseEffect;
        private RotatingSpriteEffect _bridgeEffect;
        private IDisposable _effect;

        protected override void OnDisableGameEntity()
        {
            Dispose();
        }

        public void Init(GameResourceProvider resourceProvider, uint effectParameter)
        {
#if PAL3
            // MXCEft restarts the original group when it finishes. Group 275
            // is additionally emitted every 9.5 seconds by the original scene object.
            _effect = resourceProvider.OptionalVfx.Attach((int)effectParameter, GameEntity);
#elif PAL3A
            if (effectParameter == 160) // 蜀山太极桥特效
            {
                _bridgeBaseEffect = GameEntity.AddComponent<RotatingSpriteEffect>();
                _bridgeBaseEffect.Init(resourceProvider,
                    SHUSHAN_BRIDGE_TEXTURE_NAME,
                    new Vector3(SHUSHAN_BRIDGE_DEFAULT_SIZE,
                        SHUSHAN_BRIDGE_DEFAULT_SIZE,
                        SHUSHAN_BRIDGE_DEFAULT_SIZE),
                    SHUSHAN_BRIDGE_ANIMATION_ROTATION_SPEED);

                _bridgeEffect = GameEntity.AddComponent<RotatingSpriteEffect>();
                _bridgeEffect.Init(resourceProvider,
                    SHUSHAN_BRIDGE_EFFECT_TEXTURE_NAME,
                    new Vector3(SHUSHAN_BRIDGE_EFFECT_DEFAULT_SIZE,
                        SHUSHAN_BRIDGE_EFFECT_DEFAULT_SIZE,
                        SHUSHAN_BRIDGE_EFFECT_DEFAULT_SIZE),
                    SHUSHAN_BRIDGE_EFFECT_ANIMATION_ROTATION_SPEED);
            }

            if (effectParameter == 344) // 重盘古元灵特效
            {
                _effect = resourceProvider.OptionalVfx.Attach((int)effectParameter, GameEntity);
            }
#endif
        }

        private void OnDestroy() => Dispose();

        public void Dispose()
        {
            if (_bridgeBaseEffect != null)
            {
                _bridgeBaseEffect.Dispose();
                _bridgeBaseEffect.Destroy();
                _bridgeBaseEffect = null;
            }

            if (_bridgeEffect != null)
            {
                _bridgeEffect.Dispose();
                _bridgeEffect.Destroy();
                _bridgeEffect = null;
            }

            _effect?.Dispose();
            _effect = null;
        }
    }
}
