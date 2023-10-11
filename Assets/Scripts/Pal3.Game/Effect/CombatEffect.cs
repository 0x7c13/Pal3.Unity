// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Effect
{
    using Data;
    using Engine.Abstraction;
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
        private IGameEntity _effect;

        protected override void OnDisableGameEntity()
        {
            Dispose();
        }

        public void Init(GameResourceProvider resourceProvider, uint effectParameter)
        {
            // object vfxPrefab = resourceProvider.GetVfxEffectPrefab((int)effectParameter);
            //
            // if (vfxPrefab != null)
            // {
            //     _effect = PrefabFactory.Instantiate(vfxPrefab, Transform, worldPositionStays: false);
            //     _effect.Name = "VFX_" + effectParameter;
            // }

            #if PAL3
            if (effectParameter == 465) // 蜀山太极桥特效
            #elif PAL3A
            if (effectParameter == 160) // 蜀山太极桥特效
            #endif
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

            #if PAL3
            if (effectParameter == 275) // 重楼创建的用来禁锢雪见/龙葵的法阵特效
            #elif PAL3A
            if (effectParameter == 344) // 重盘古元灵特效
            #endif
            {
                object vfxPrefab = resourceProvider.GetVfxEffectPrefab((int)effectParameter);
                if (vfxPrefab != null)
                {
                    _effect = PrefabFactory.Instantiate(vfxPrefab, Transform, worldPositionStays: false);
                    _effect.Name = "VFX_" + effectParameter;
                }
            }
        }

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

            if (_effect != null)
            {
                _effect.Destroy();
                _effect = null;
            }
        }
    }
}