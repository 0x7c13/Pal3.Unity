// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.UI
{
    using UnityEngine;
    using UnityEngine.UI;

    [RequireComponent(typeof(RectTransform))]
    public class RoundedFrostedGlassImage : MonoBehaviour
    {
        private static readonly int BlurAmountPropertyId = Shader.PropertyToID("_BlurAmount");
        private static readonly int WidthHeightRadiusPropertyId = Shader.PropertyToID("_WidthHeightRadius");
        private static readonly int AdditiveColorPropertyId = Shader.PropertyToID("_AdditiveColor");
        private static readonly int MultiplyColorPropertyId = Shader.PropertyToID("_MultiplyColor");

        private static Shader _shader;

        [SerializeField] public float blurAmount = 1;
        [SerializeField] public float cornerRadius;
        [SerializeField] public Color additiveTintColor = Color.black;
        [SerializeField] public Color multiplyTintColor = Color.white;

        private Material _material;

        [HideInInspector, SerializeField] private MaskableGraphic image;

        private void OnValidate()
        {
            Init();
            UpdateLayout();
        }

        private void OnEnable()
        {
            Init();
            UpdateLayout();
        }

        private void Start()
        {
            Init();
            UpdateLayout();
        }

        private void OnDestroy()
        {
            Destroy(_material);
            _material = null;
            image = null;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (enabled && _material != null)
            {
                UpdateLayout();
            }
        }

        private void Init()
        {
            if (_shader == null)
            {
                _shader = Shader.Find("Pal3/RoundedFrostedGlass");
                if (_shader == null)
                {
                    Debug.LogError("Shader not found: Pal3/RoundedFrostedGlass");
                    return;
                }
            }

            if (_material == null)
            {
                _material = new Material(_shader);
            }

            if (image == null)
            {
                TryGetComponent(out image);
            }

            if (image != null)
            {
                image.material = _material;
            }
        }

        private void UpdateLayout()
        {
            if (_material == null) return;

            Rect rect = ((RectTransform)transform).rect;
            _material.SetFloat(BlurAmountPropertyId, blurAmount);
            _material.SetColor(AdditiveColorPropertyId, additiveTintColor);
            _material.SetColor(MultiplyColorPropertyId, multiplyTintColor);
            _material.SetVector(WidthHeightRadiusPropertyId,
                new Vector4(rect.width, rect.height, cornerRadius, 0));
        }
    }
}