// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Engine.UI
{
    using System.Collections.Generic;
    using Extensions;
    using Logging;
    using UnityEngine;
    using UnityEngine.UI;

    [RequireComponent(typeof(RectTransform))]
    public class RoundedFrostedGlassImage : MonoBehaviour
    {
        private const string ShaderName = "Pal3/RoundedFrostedGlass";

        private static readonly int BlurAmountPropertyId = Shader.PropertyToID("_BlurAmount");
        private static readonly int TransparencyPropertyId = Shader.PropertyToID("_Transparency");
        private static readonly int WidthHeightRadiusPropertyId = Shader.PropertyToID("_WidthHeightRadius");
        private static readonly int AdditiveColorPropertyId = Shader.PropertyToID("_AdditiveColor");
        private static readonly int MultiplyColorPropertyId = Shader.PropertyToID("_MultiplyColor");

        private static readonly HashSet<RoundedFrostedGlassImage> EnabledPanels = new();
        private static Shader _shader;

        [SerializeField] public float blurAmount = 1f;
        [SerializeField] public float transparency = 1f;
        [SerializeField] public float cornerRadius;
        [SerializeField] public Color additiveTintColor = Color.black;
        [SerializeField] public Color multiplyTintColor = Color.white;

        private Material _material;

        [HideInInspector, SerializeField] private MaskableGraphic image;

        public static bool HasVisibleOverlay(int targetDisplay)
        {
            foreach (var panel in EnabledPanels)
            {
                var graphic = panel.image;
                if (graphic == null || !graphic.isActiveAndEnabled || graphic.color.a <= 0f ||
                    graphic.canvasRenderer.GetInheritedAlpha() <= 0f)
                {
                    continue;
                }

                Canvas canvas = graphic.canvas;
                if (canvas != null && canvas.isActiveAndEnabled &&
                    canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay &&
                    canvas.rootCanvas.targetDisplay == targetDisplay)
                {
                    return true;
                }
            }

            return false;
        }

        public void SetMaterialBlurAmount(float newBlurAmount)
        {
            if (_material == null) return;
            _material.SetFloat(BlurAmountPropertyId, newBlurAmount);
        }

        public void SetMaterialTransparency(float newAlpha)
        {
            if (_material == null) return;
            _material.SetFloat(TransparencyPropertyId, newAlpha);
        }

        private void OnValidate()
        {
            Init();
            UpdateLayout();
        }

        private void OnEnable()
        {
            Init();
            UpdateLayout();
            EnabledPanels.Add(this);
        }

        private void OnDisable()
        {
            EnabledPanels.Remove(this);
        }

        private void Start()
        {
            Init();
            UpdateLayout();
        }

        private void OnDestroy()
        {
            _material.Destroy();
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
                _shader = Shader.Find(ShaderName);
                if (_shader == null)
                {
                    EngineLogger.LogError($"Shader not found: {ShaderName}");
                    return;
                }
            }

            if (_material == null)
            {
                _material = new Material(_shader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
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
            _material.SetFloat(TransparencyPropertyId, transparency);
            _material.SetColor(AdditiveColorPropertyId, additiveTintColor);
            _material.SetColor(MultiplyColorPropertyId, multiplyTintColor);
            _material.SetVector(WidthHeightRadiusPropertyId,
                new Vector4(rect.width, rect.height, cornerRadius, 0));
        }
    }
}