// See LICENSE in the project root.

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

        private Camera _camera;
        private GameSettings _settings;
        private Object _nightScene;

        internal bool ContactShadowsEnabled => _settings?.ToonContactShadows == true;

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

        internal void SetNightScene(Object scene) => _nightScene = scene;

        internal void ClearNightScene(Object scene)
        {
            if (_nightScene == scene) _nightScene = null;
        }

        public void Detach()
        {
            _settings = null;
            _nightScene = null;
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
        }

        private void ApplyLightingSettings(ScriptableRenderContext context, Camera camera)
        {
            if (camera == _camera)
            {
                Shader.SetGlobalFloat(CharacterLightingId, _settings?.ToonCharacterLighting == true ? 1f : 0f);
                Shader.SetGlobalFloat(NightFireBoostId,
                    _nightScene != null && _settings?.ToonNightFireBoost == true ? 1f : 0f);
            }
        }
    }
}
