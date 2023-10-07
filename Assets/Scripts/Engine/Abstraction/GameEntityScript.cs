// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Abstraction
{
    using System.Collections;
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

        protected virtual void OnDestroyGameEntity() {}

        private void OnEnable()
        {
            OnEnableGameEntity();
        }

        private void OnDisable()
        {
            OnDisableGameEntity();
        }

        private void OnDestroy()
        {
            OnDestroyGameEntity();
        }
    }
}