// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects.Common
{
    using System;
    using UnityEngine;

    public class StandingPlatformController : MonoBehaviour
    {
        public event EventHandler<Collider> OnTriggerEntered;

        public int LayerIndex { get; private set; }

        private BoxCollider _collider;
        private Bounds _triggerBounds;

        public void SetBounds(Bounds triggerBounds, int layerIndex)
        {
            _triggerBounds = triggerBounds;
            LayerIndex = layerIndex;

            if (_collider == null)
            {
                _collider = gameObject.AddComponent<BoxCollider>();   
            }
            
            _collider.center = _triggerBounds.center;
            _collider.size = _triggerBounds.size;
            _collider.isTrigger = true;
        }

        public Collider GetCollider()
        {
            return _collider;
        }

        public float GetPlatformHeight()
        {
            // A little bit lower than the collider bounds just to make sure
            // the actor is always inside the collider when standing on the platform.
            return _collider.bounds.max.y - 0.05f;
        }

        private void OnTriggerEnter(Collider other)
        {
            OnTriggerEntered?.Invoke(this, other);
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