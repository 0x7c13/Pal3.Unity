// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Utils
{
    using UnityEngine;

    public class FpsCounter : MonoBehaviour
    {
        private int _frameCount = 0;
        private float _deltaTime = 0f;
        private float _fps = 0f;
        private float _updateRate = 15f;  // 15 updates per sec.

        private void Update()
        {
            _frameCount++;
            _deltaTime += Time.deltaTime / Time.timeScale;

            if (_deltaTime > (1.0 / _updateRate))
            {
                _fps = _frameCount / _deltaTime;
                _frameCount = 0;
                _deltaTime -= 1f / _updateRate;
            }
        }

        public float GetFps()
        {
            return _fps;
        }
    }
}