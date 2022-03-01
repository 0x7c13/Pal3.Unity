// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Renderer
{
    using UnityEngine;

    public class StaticBillboardRenderer : MonoBehaviour
    {
        private Camera _camera;

        private void OnEnable()
        {
            _camera = Camera.main;
        }

        private void LateUpdate()
        {
            var rotation = _camera.transform.rotation;
            rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
            transform.rotation = rotation;
        }
    }
}