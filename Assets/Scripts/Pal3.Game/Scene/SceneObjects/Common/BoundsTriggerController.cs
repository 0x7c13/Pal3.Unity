// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene.SceneObjects.Common
{
    using System;
    using Actor.Controllers;
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
    using Engine.Extensions;
    using Engine.Services;
    using GamePlay;
    using UnityEngine;

    public sealed class BoundsTriggerController : GameEntityScript
    {
        public event EventHandler<IGameEntity> OnPlayerActorEntered;
        public event EventHandler<IGameEntity> OnPlayerActorExited;

        private bool _hasCollided;
        private BoxCollider _collider;

        private PlayerActorManager _playerActorManager;

        public void SetBounds(Bounds bounds, bool isTrigger)
        {
            if (_collider == null)
            {
                _collider = GameEntity.AddComponent<BoxCollider>();
            }

            _collider.center = bounds.center;
            _collider.size = bounds.size;
            _collider.isTrigger = isTrigger;
        }

        protected override void OnEnableGameEntity()
        {
            _playerActorManager = ServiceLocator.Instance.Get<PlayerActorManager>();
        }

        protected override void OnDisableGameEntity()
        {
            if (_collider != null)
            {
                _collider.Destroy();
                _collider = null;
            }
        }

        protected override void OnCollisionEnterGameEntity(IGameEntity otherGameEntity)
        {
            if (otherGameEntity.GetComponent<ActorController>() is {} actorController &&
                actorController.GetActor().Id == _playerActorManager.GetPlayerActorId())
            {
                OnPlayerActorEntered?.Invoke(this, otherGameEntity);
            }
        }

        protected override void OnTriggerEnterGameEntity(IGameEntity otherGameEntity)
        {
            if (otherGameEntity.GetComponent<ActorController>() is {} actorController &&
                actorController.GetActor().Id == _playerActorManager.GetPlayerActorId())
            {
                OnPlayerActorEntered?.Invoke(this, otherGameEntity);
            }
        }

        protected override void OnCollisionExitGameEntity(IGameEntity otherGameEntity)
        {
            if (otherGameEntity.GetComponent<ActorController>() is {} actorController &&
                actorController.GetActor().Id == _playerActorManager.GetPlayerActorId())
            {
                OnPlayerActorExited?.Invoke(this, otherGameEntity);
            }
        }

        protected override void OnTriggerExitGameEntity(IGameEntity otherGameEntity)
        {
            if (otherGameEntity.GetComponent<ActorController>() is {} actorController &&
                actorController.GetActor().Id == _playerActorManager.GetPlayerActorId())
            {
                OnPlayerActorExited?.Invoke(this, otherGameEntity);
            }
        }
    }
}