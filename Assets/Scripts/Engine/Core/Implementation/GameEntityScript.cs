// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Core.Implementation
{
    using System;
    using System.Collections;
    using Abstraction;
    using UnityEngine;

    public abstract class GameEntityScript : MonoBehaviour
    {
        private ITransform _transform;
        private IGameEntity _gameEntity;

        public ITransform Transform
        {
            get { return _transform ??= new Transform(base.transform); }
        }

        public IGameEntity GameEntity
        {
            get { return _gameEntity ??= new GameEntity(base.gameObject); }
        }

        protected new void StartCoroutine(IEnumerator coroutine)
        {
            base.StartCoroutine(coroutine);
        }

        protected virtual void OnEnableGameEntity() {}

        protected virtual void OnDisableGameEntity() {}

        protected virtual void OnCollisionEnterGameEntity(IGameEntity other) {}

        protected virtual void OnCollisionExitGameEntity(IGameEntity other) {}

        protected virtual void OnTriggerEnterGameEntity(IGameEntity other) {}

        protected virtual void OnTriggerExitGameEntity(IGameEntity other) {}

        private void OnEnable()
        {
            OnEnableGameEntity();
        }

        private void OnDisable()
        {
            OnDisableGameEntity();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject != null)
            {
                OnCollisionEnterGameEntity(new GameEntity(collision.gameObject));
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject != null)
            {
                OnCollisionExitGameEntity(new GameEntity(collision.gameObject));
            }
        }

        private void OnTriggerEnter(Collider otherCollider)
        {
            if (otherCollider.gameObject != null)
            {
                OnTriggerEnterGameEntity(new GameEntity(otherCollider.gameObject));
            }
        }

        private void OnTriggerExit(Collider otherCollider)
        {
            if (otherCollider.gameObject != null)
            {
                OnTriggerExitGameEntity(new GameEntity(otherCollider.gameObject));
            }
        }
    }
}