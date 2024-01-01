// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Renderer
{
    using Core.Implementation;
    using UnityEngine;

    public class StaticBillboardRenderer : TickableGameEntityScript
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
            Transform.Rotation = rotation;
        }
    }
}