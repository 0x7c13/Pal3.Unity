// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Toon
{
    using Settings;
    using UnityEngine;
    using UnityEngine.Rendering.Universal;

    public static class ToonLighting
    {
        private static readonly int SceneLayers = LayerMask.GetMask("Default", "VFX");

        public static Light CreateKey(Transform parent, Quaternion rotation, Color color, float intensity,
            float shadowStrength = 0.85f)
        {
            var source = new GameObject("Toon Key Light");
            source.transform.SetParent(parent, false);
            source.transform.rotation = rotation;

            var light = source.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.cullingMask = SceneLayers;
            light.shadows = shadowStrength > 0f ? LightShadows.Soft : LightShadows.None;
            light.shadowStrength = shadowStrength;
            return light;
        }

        public static void ConfigureKeyShadows(Light light, GameSettings settings)
        {
            light.shadowStrength = settings.ToonShadowStrength;
            light.shadows = settings.ToonRealtimeShadows && light.shadowStrength > 0f
                ? LightShadows.Soft : LightShadows.None;
        }

        internal static Light CreateFireFill(Transform parent, float height, bool enhanced,
            bool indoors, GameSettings settings)
        {
            var source = new GameObject("Toon Fire Fill");
            source.transform.SetParent(parent, false);
            source.transform.localPosition = Vector3.up * height;

            var light = source.AddComponent<Light>();
            light.type = LightType.Point;
            light.cullingMask = SceneLayers;
            light.shadowNearPlane = 0.25f;
            light.GetUniversalAdditionalLightData().additionalLightsShadowResolutionTier =
                UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow;
            ConfigureFireFill(light, enhanced, indoors, settings);
            return light;
        }

        internal static void ConfigureFireFill(Light light, bool enhanced, bool indoors, GameSettings settings)
        {
            light.color = enhanced ? new Color(1f, 0.58f, 0.29f) : new Color(1f, 0.62f, 0.36f);
            float intensity = enhanced ? (indoors ? 24f : 20f) : 2.4f;
            float range = enhanced ? (indoors ? 32f : 28f) : 8f;
            light.intensity = settings.ToonFireLighting ? intensity * settings.ToonFireIntensityScale : 0f;
            light.range = Mathf.Max(0.01f, range * settings.ToonFireRangeScale);
            light.shadowStrength = settings.ToonShadowStrength;
        }
    }
}
