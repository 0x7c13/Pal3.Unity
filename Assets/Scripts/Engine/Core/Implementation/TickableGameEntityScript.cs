// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Core.Implementation
{
    using UnityEngine;

    public abstract class TickableGameEntityScript : GameEntityScript
    {
        protected virtual void OnUpdateGameEntity(float deltaTime) {}

        protected virtual void OnLateUpdateGameEntity(float deltaTime) {}

        protected virtual void OnFixedUpdateGameEntity(float fixedDeltaTime) {}

        private void Update()
        {
            OnUpdateGameEntity(Time.deltaTime);
        }

        private void LateUpdate()
        {
            OnLateUpdateGameEntity(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            OnFixedUpdateGameEntity(Time.fixedDeltaTime);
        }
    }
}