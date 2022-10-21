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

    public class PortalEffect : MonoBehaviour, IEffect, IDisposable
    {
        #if PAL3
        private const string PORTAL_BASE_TEXTURE_NAME = "trans.dds";
        #elif PAL3A
        private const string PORTAL_BASE_TEXTURE_NAME = "trans.tga";
        #endif
        
        private const float PORTAL_DEFAULT_SIZE = 1.3f;
        private const float PORTAL_ANIMATION_ROTATION_SPEED = 5f;

        private RotatingSpriteEffect _baseEffect;

        public void Init(GameResourceProvider resourceProvider, uint _)
        {
            _baseEffect = gameObject.AddComponent<RotatingSpriteEffect>();
            _baseEffect.Init(resourceProvider,
                PORTAL_BASE_TEXTURE_NAME,
                new Vector3(PORTAL_DEFAULT_SIZE, PORTAL_DEFAULT_SIZE, PORTAL_DEFAULT_SIZE),
                PORTAL_ANIMATION_ROTATION_SPEED);
        }

        private void OnDisable()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_baseEffect != null)
            {
                _baseEffect.Dispose();
            }
        }
    }
}