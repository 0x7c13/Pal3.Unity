// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Renderer
{
    using Abstraction;
    using UnityEngine;

    public class StaticBillboardRenderer : TickableGameEntityBase
    {
        private Camera _camera;

        protected override void OnEnableGameEntity()
        {
            _camera = Camera.main;
        }

        protected override void OnLateUpdateGameEntity(float deltaTime)
        {
            Quaternion rotation = _camera.transform.rotation;
            rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
            transform.rotation = rotation;
        }
    }
}