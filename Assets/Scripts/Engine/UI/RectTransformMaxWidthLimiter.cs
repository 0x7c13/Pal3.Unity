// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.UI
{
    using UnityEngine;

    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class RectTransformMaxWidthLimiter : MonoBehaviour
    {
        [SerializeField] public float thresholdRatio;
        [SerializeField] public float padding;

        private RectTransform _transform;
        private Canvas _canvas;

        private void OnEnable()
        {
            _transform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            var currentRatio = (float) Screen.width / Screen.height;
            _transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentRatio > thresholdRatio
                ? _transform.rect.height * thresholdRatio - padding
                : _transform.rect.height * currentRatio - padding);
        }

        private void OnValidate() => OnEnable();
    }
}