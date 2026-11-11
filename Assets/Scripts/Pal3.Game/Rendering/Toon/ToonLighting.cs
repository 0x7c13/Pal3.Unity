// See LICENSE in the project root.

namespace Pal3.Game.Rendering.Toon
{
    using UnityEngine;
    using UnityEngine.Rendering.Universal;

    public static class ToonLighting
    {
        private static readonly int SceneLayers = LayerMask.GetMask("Default", "VFX");

        public static Light CreateKey(Transform parent, Quaternion rotation, Color color, float intensity)
        {
            var source = new GameObject("Toon Key Light");
            source.transform.SetParent(parent, false);
            source.transform.rotation = rotation;

            var light = source.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.cullingMask = SceneLayers;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.85f;
            return light;
        }

        internal static Light CreateFireFill(Transform parent, float height, bool enhanced)
        {
            var source = new GameObject("Toon Fire Fill");
            source.transform.SetParent(parent, false);
            source.transform.localPosition = Vector3.up * height;

            var light = source.AddComponent<Light>();
            light.type = LightType.Point;
            light.cullingMask = SceneLayers;
            light.shadowStrength = 0.85f;
            light.shadowNearPlane = 0.25f;
            light.GetUniversalAdditionalLightData().additionalLightsShadowResolutionTier =
                UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow;
            ConfigureFireFill(light, enhanced);
            return light;
        }

        internal static void ConfigureFireFill(Light light, bool enhanced)
        {
            light.color = enhanced ? new Color(1f, 0.58f, 0.29f) : new Color(1f, 0.62f, 0.36f);
            light.intensity = enhanced ? 14f : 2.4f;
            light.range = enhanced ? 24f : 8f;
        }
    }
}
