// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Utilities
{
    using UnityEngine;

    public class FpsCounter : MonoBehaviour
    {
        private const float UPDATE_INTERVAL = 0.3f;

        private int _frameCount = 0;
        private float _deltaTime = 0f;
        private float _fps = 0f;

        private void Update()
        {
            _frameCount++;
            _deltaTime += Time.unscaledDeltaTime;

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