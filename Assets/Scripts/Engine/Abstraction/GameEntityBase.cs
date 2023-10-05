// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Abstraction
{
    using UnityEngine;

    public abstract class GameEntityBase : MonoBehaviour
    {
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

    public abstract class TickableGameEntityBase : GameEntityBase
    {
        protected virtual void OnUpdateGameEntity(float deltaTime) {}

        protected virtual void OnLateUpdateGameEntity(float deltaTime) {}

        protected virtual void OnFixedUpdateGameEntity(float fixedDeltaTime) {}

        private void Update()
        {
            OnUpdateGameEntity(Time.deltaTime);
        }

        protected void LateUpdate()
        {
            OnLateUpdateGameEntity(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            OnFixedUpdateGameEntity(Time.fixedDeltaTime);
        }
    }
}