// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Renderer
{
    using Core.Abstraction;
    using Core.Implementation;
    using Extensions;
    using UnityEngine;

    public class SkyBoxRenderer : GameEntityScript
    {
        private Skybox _skybox;

        protected override void OnDisableGameEntity()
        {
            if (_skybox != null)
            {
                _skybox.material.Destroy();
                _skybox.Destroy();
                _skybox = null;
            }
        }

        public void Render(IGameEntity cameraEntity,
            IMaterial skyboxMaterial)
        {
            _skybox = cameraEntity.AddComponent<Skybox>();
            _skybox.material = skyboxMaterial.NativeObject as Material;
        }
    }
}