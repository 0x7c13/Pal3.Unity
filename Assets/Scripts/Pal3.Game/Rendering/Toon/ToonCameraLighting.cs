// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Toon
{
    using Settings;
    using UnityEngine;
    using UnityEngine.Rendering;
    using Camera = UnityEngine.Camera;

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class ToonCameraLighting : MonoBehaviour
    {
        private static readonly int CharacterLightingId = Shader.PropertyToID("_Pal3ToonCharacterLighting");

        private static readonly int NightFireBoostId = Shader.PropertyToID("_Pal3ToonNightFireBoost");

        private static readonly int SceneLightingId = Shader.PropertyToID("_Pal3ToonSceneLighting");
        private static readonly int EffectStrengthsId = Shader.PropertyToID("_Pal3ToonEffectStrengths");

        private Camera _camera;
        private GameSettings _settings;
        private Object _scene;
        private bool _night;
        private bool _indoors;

        internal bool ContactShadowsEnabled => _settings?.ToonContactShadows == true && _settings.ToonContactShadowStrength > 0f;

        public static ToonCameraLighting Attach(Camera camera, GameSettings settings)
        {
            if (!camera.TryGetComponent(out ToonCameraLighting lighting))
            {
                lighting = camera.gameObject.AddComponent<ToonCameraLighting>();
            }

            lighting._settings = settings;
            lighting.enabled = true;
            return lighting;
        }

        internal void SetScene(Object scene, bool night, bool indoors)
        {
            _scene = scene;
            _night = night;
            _indoors = indoors;
        }

        internal void ClearScene(Object scene)
        {
            if (_scene == scene) _scene = null;
        }

        public void Detach()
        {
            _settings = null;
            _scene = null;
            enabled = false;
        }

        private void OnEnable()
        {
            _camera = GetComponent<Camera>();
            RenderPipelineManager.beginCameraRendering += ApplyLightingSettings;
            RenderPipelineManager.endCameraRendering += ClearLightingSettings;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= ApplyLightingSettings;
            RenderPipelineManager.endCameraRendering -= ClearLightingSettings;
        }

        private void ClearLightingSettings(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _camera) return;
            Shader.SetGlobalFloat(CharacterLightingId, 0f);
            Shader.SetGlobalFloat(NightFireBoostId, 0f);
            Shader.SetGlobalVector(SceneLightingId, Vector4.zero);
            Shader.SetGlobalVector(EffectStrengthsId, Vector4.zero);
        }

        private void ApplyLightingSettings(ScriptableRenderContext context, Camera camera)
        {
            if (camera == _camera)
            {
                Shader.SetGlobalFloat(CharacterLightingId, _settings?.ToonCharacterLighting == true
                    ? _settings.ToonCharacterLightingStrength : 0f);
                Shader.SetGlobalFloat(NightFireBoostId,
                    _scene != null && _night && _settings?.ToonNightFireBoost == true ? 1f : 0f);
                Shader.SetGlobalVector(SceneLightingId, new Vector4(
                    _scene != null && _night ? 1f : 0f,
                    _scene != null && _indoors ? 1f : 0f,
                    _scene != null && _settings?.ToonEnvironmentContrast == true
                        ? _settings.ToonEnvironmentContrastStrength : 0f,
                    0f));
                Shader.SetGlobalVector(EffectStrengthsId, new Vector4(
                    ContactShadowsEnabled ? _settings.ToonContactShadowStrength : 0f,
                    _settings?.ToonBakedShadows == true ? _settings.ToonBakedShadowStrength : 0f, 0f, 0f));
            }
        }
    }
}
