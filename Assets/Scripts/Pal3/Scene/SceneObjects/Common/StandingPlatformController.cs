// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects.Common
{
    using System;
    using Actor;
    using Core.Services;
    using Player;
    using UnityEngine;

    public class StandingPlatformController : MonoBehaviour
    {
        public event EventHandler<GameObject> OnPlayerActorEntered;
        public event EventHandler<GameObject> OnPlayerActorExited;

        public int LayerIndex { get; private set; }

        private BoxCollider _collider;
        private Bounds _triggerBounds;
        private float _heightOffset;

        private PlayerManager _playerManager;

        private void OnEnable()
        {
            _playerManager = ServiceLocator.Instance.Get<PlayerManager>();
        }

        public void SetBounds(Bounds triggerBounds, int layerIndex, float heightOffset = 0f)
        {
            _triggerBounds = triggerBounds;
            LayerIndex = layerIndex;
            _heightOffset = heightOffset;

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
            return _collider.bounds.max.y + _heightOffset;
        }

        private void OnTriggerEnter(Collider otherCollider)
        {
            if (otherCollider.gameObject.GetComponent<ActorController>() is { } actorController &&
                actorController.GetActor().Info.Id == (byte) _playerManager.GetPlayerActor())
            {
                OnPlayerActorEntered?.Invoke(this, otherCollider.gameObject);
            }
        }

        private void OnTriggerExit(Collider otherCollider)
        {
            if (otherCollider.gameObject.GetComponent<ActorController>() is { } actorController &&
                actorController.GetActor().Info.Id == (byte) _playerManager.GetPlayerActor())
            {
                OnPlayerActorExited?.Invoke(this, otherCollider.gameObject);
            }
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