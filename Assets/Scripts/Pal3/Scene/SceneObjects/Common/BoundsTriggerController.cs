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

    public class BoundsTriggerController : MonoBehaviour
    {
        public event EventHandler<GameObject> OnPlayerActorEntered;
        public event EventHandler<GameObject> OnPlayerActorExited;

        private bool _hasCollided;
        private BoxCollider _collider;

        private PlayerManager _playerManager;

        public void SetupCollider(Bounds bounds, bool isTrigger)
        {
            if (_collider == null)
            {
                _collider = gameObject.AddComponent<BoxCollider>();
            }

            _collider.center = bounds.center;
            _collider.size = bounds.size;
            _collider.isTrigger = isTrigger;
        }

        private void OnEnable()
        {
            _playerManager = ServiceLocator.Instance.Get<PlayerManager>();
        }

        private void OnDisable()
        {
            if (_collider != null)
            {
                Destroy(_collider);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.GetComponent<ActorController>() is {} actorController &&
                actorController.GetActor().Info.Id == (byte) _playerManager.GetPlayerActor())
            {
                OnPlayerActorEntered?.Invoke(this, collision.gameObject);
            }
        }

        private void OnTriggerEnter(Collider collider)
        {
            if (collider.gameObject.GetComponent<ActorController>() is {} actorController &&
                actorController.GetActor().Info.Id == (byte) _playerManager.GetPlayerActor())
            {
                OnPlayerActorEntered?.Invoke(this, collider.gameObject);
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject.GetComponent<ActorController>() is {} actorController &&
                actorController.GetActor().Info.Id == (byte) _playerManager.GetPlayerActor())
            {
                OnPlayerActorExited?.Invoke(this, collision.gameObject);
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            if (collider.gameObject.GetComponent<ActorController>() is {} actorController &&
                actorController.GetActor().Info.Id == (byte) _playerManager.GetPlayerActor())
            {
                OnPlayerActorExited?.Invoke(this, collider.gameObject);
            }
        }
    }
}