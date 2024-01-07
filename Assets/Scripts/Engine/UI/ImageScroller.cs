// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.UI
{
    using UnityEngine;
    using UnityEngine.UI;

    public class ImageScroller : MonoBehaviour
    {
        [SerializeField] private RawImage image;
        [SerializeField] private float scrollSpeed = 0.05f;

        private void Update()
        {
            Rect offset = image.uvRect;
            offset.x += scrollSpeed * Time.deltaTime;
            image.uvRect = offset;
        }
    }
}