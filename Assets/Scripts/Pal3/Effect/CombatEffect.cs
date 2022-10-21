// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Effect
{
    using System;
    using Data;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public class CombatEffect : MonoBehaviour, IEffect, IDisposable
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

        public void Init(GameResourceProvider resourceProvider, uint effectParameter)
        {
            // Object vfxPrefab = resourceProvider.GetVfxEffectPrefab((int)effectParameter);
            //
            // if (vfxPrefab != null)
            // {
            //     _effect = (GameObject)Instantiate(vfxPrefab, transform, false);
            //     _effect.name = "VFX_" + effectParameter;
            // }
            
            #if PAL3
            if (effectParameter == 465) // 蜀山太极桥特效
            #elif PAL3A
            if (effectParameter == 160) // 蜀山太极桥特效
            #endif
            {
                _bridgeBaseEffect = gameObject.AddComponent<RotatingSpriteEffect>();
                _bridgeBaseEffect.Init(resourceProvider,
                    SHUSHAN_BRIDGE_TEXTURE_NAME,
                    new Vector3(SHUSHAN_BRIDGE_DEFAULT_SIZE,
                        SHUSHAN_BRIDGE_DEFAULT_SIZE,
                        SHUSHAN_BRIDGE_DEFAULT_SIZE),
                    SHUSHAN_BRIDGE_ANIMATION_ROTATION_SPEED);
                
                _bridgeEffect = gameObject.AddComponent<RotatingSpriteEffect>();
                _bridgeEffect.Init(resourceProvider,
                    SHUSHAN_BRIDGE_EFFECT_TEXTURE_NAME,
                    new Vector3(SHUSHAN_BRIDGE_EFFECT_DEFAULT_SIZE,
                        SHUSHAN_BRIDGE_EFFECT_DEFAULT_SIZE,
                        SHUSHAN_BRIDGE_EFFECT_DEFAULT_SIZE),
                    SHUSHAN_BRIDGE_EFFECT_ANIMATION_ROTATION_SPEED);
            }
            else
            {
                Debug.LogWarning("Combat effect for scene object not implemented: " + effectParameter);
            }
        }
        
        private void OnDisable()
        {
            Dispose();
        }
        
        public void Dispose()
        {
            if (_bridgeBaseEffect != null)
            {
                _bridgeBaseEffect.Dispose();
            }
            
            if (_bridgeEffect != null)
            {
                _bridgeEffect.Dispose();
            }
        }
    }
}