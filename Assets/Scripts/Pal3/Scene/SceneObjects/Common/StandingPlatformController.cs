// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects.Common
{
    using Core.Extensions;
    using UnityEngine;

    public class StandingPlatformController : MonoBehaviour
    {
        private BoxCollider _collider;
        private Bounds _triggerBounds;
        private Bounds _rendererBounds;

        public void SetBounds(Bounds triggerBounds, Bounds rendererBounds)
        {
            _triggerBounds = triggerBounds;
            _rendererBounds = rendererBounds;

            _collider = gameObject.GetOrAddComponent<BoxCollider>();
            _collider.center = _triggerBounds.center;
            _collider.size = _triggerBounds.size;
            _collider.isTrigger = true;
        }

        public Bounds GetRendererBounds()
        {
            return _rendererBounds;
        }
        
        public float GetPlatformHeight()
        {
            return _rendererBounds.max.y;
        }
        
        private void OnDisable()
        {
            if (_collider != null)
            {
                Destroy(_collider);   
            }
        }
    }
}