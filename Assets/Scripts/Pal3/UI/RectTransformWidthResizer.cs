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
        public float thresholdRatio;
        public float minWidth;
        public float maxWidth;

        private RectTransform _transform;
        private Canvas _canvas;

        void OnEnable()
        {
            _transform = GetComponent<RectTransform>();
        }

        void Update()
        {
            bool useMinWidth = (float)Screen.width / Screen.height < thresholdRatio;
            _transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, useMinWidth ? minWidth : maxWidth);
        }

        void OnValidate() => OnEnable();
    }
}