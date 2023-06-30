// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.UI
{
    using UnityEngine;

    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class RectTransformWidthResizer : MonoBehaviour
    {
        public float maxThresholdRatio;
        public float minThresholdRatio;

        public float minWidth;
        public float midWidth;
        public float maxWidth;

        private RectTransform _transform;
        private Canvas _canvas;

        void OnEnable()
        {
            _transform = GetComponent<RectTransform>();
        }

        void Update()
        {
            float currentRatio = (float) Screen.width / Screen.height;

            bool useMaxWidth = currentRatio > maxThresholdRatio;
            bool useMinWidth = currentRatio < minThresholdRatio;

            _transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, useMaxWidth ? maxWidth : useMinWidth ? minWidth : midWidth);
        }

        void OnValidate() => OnEnable();
    }
}