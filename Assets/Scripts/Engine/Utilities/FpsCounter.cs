// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Utilities
{
    using Abstraction;
    using UnityEngine;

    public class FpsCounter : TickableGameEntityBase
    {
        private const float UPDATE_INTERVAL = 0.3f;

        private int _frameCount = 0;
        private float _deltaTime = 0f;
        private float _fps = 0f;

        protected override void OnUpdateGameEntity(float deltaTime)
        {
            _frameCount++;
            _deltaTime += deltaTime / Time.timeScale;

            if (_deltaTime >= UPDATE_INTERVAL)
            {
                _fps = _frameCount / _deltaTime;
                _frameCount = 0;
                _deltaTime -= UPDATE_INTERVAL;
            }
        }

        public float GetFps()
        {
            return _fps;
        }
    }
}