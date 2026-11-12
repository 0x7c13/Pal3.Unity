// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Rendering.ScreenEffects
{
    using Engine.UI;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.RenderGraphModule;
    using UnityEngine.Rendering.RenderGraphModule.Util;
    using UnityEngine.Rendering.Universal;

    public sealed class Pal3ScreenEffectsFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader distortionShader;
        [SerializeField] private Shader snowShader;

        private Material _distortion;
        private Material _snow;
        private ScreenEffectsPass _pass;

        public override void Create()
        {
            CoreUtils.Destroy(_distortion);
            CoreUtils.Destroy(_snow);
            _distortion = CoreUtils.CreateEngineMaterial(distortionShader);
            _snow = CoreUtils.CreateEngineMaterial(snowShader);
            _pass = new ScreenEffectsPass(_distortion, _snow)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing,
                requiresIntermediateTexture = true,
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;
            if (cameraData.camera.TryGetComponent(out Pal3ScreenEffects effects) && effects.isActiveAndEnabled &&
                (effects.Distortion || effects.Snow ||
                 (cameraData.rendersOverlayUI && RoundedFrostedGlassImage.HasVisibleOverlay(cameraData.camera.targetDisplay))))
            {
                renderer.EnqueuePass(_pass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_distortion);
            CoreUtils.Destroy(_snow);
        }

        private sealed class ScreenEffectsPass : ScriptableRenderPass
        {
            private static readonly int SceneColorId = Shader.PropertyToID("_Pal3SceneColor");
            private readonly Material _distortion;
            private readonly Material _snow;

            public ScreenEffectsPass(Material distortion, Material snow)
            {
                _distortion = distortion;
                _snow = snow;
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                UniversalResourceData resources = frameData.Get<UniversalResourceData>();
                if (resources.isActiveTargetBackBuffer) return;

                var cameraData = frameData.Get<UniversalCameraData>();
                Pal3ScreenEffects effects = cameraData.camera.GetComponent<Pal3ScreenEffects>();
                if (effects.Distortion)
                {
                    resources.cameraColor = Apply(graph, resources.cameraColor, _distortion, "Pal3 Distortion");
                }

                if (effects.Snow)
                {
                    resources.cameraColor = Apply(graph, resources.cameraColor, _snow, "Pal3 Snow");
                }

                if (cameraData.rendersOverlayUI && RoundedFrostedGlassImage.HasVisibleOverlay(cameraData.camera.targetDisplay))
                {
                    // The overlay canvas samples the finished scene, including transparent objects.
                    TextureHandle source = resources.cameraColor;
                    TextureDesc descriptor = source.GetDescriptor(graph);
                    descriptor.name = "Pal3 Frosted Glass Backdrop";
                    descriptor.sizeMode = TextureSizeMode.Explicit;
                    descriptor.width = Mathf.Max(1, cameraData.cameraTargetDescriptor.width / 2);
                    descriptor.height = Mathf.Max(1, cameraData.cameraTargetDescriptor.height / 2);
                    descriptor.msaaSamples = MSAASamples.None;
                    descriptor.bindTextureMS = false;
                    descriptor.clearBuffer = false;
                    TextureHandle copy = graph.CreateTexture(descriptor);
                    using var builder = graph.AddBlitPass(source, copy, Vector2.one, Vector2.zero,
                        filterMode: RenderGraphUtils.BlitFilterMode.ClampBilinear,
                        passName: descriptor.name, returnBuilder: true);
                    builder.SetGlobalTextureAfterPass(copy, SceneColorId);
                }
            }

            private static TextureHandle Apply(RenderGraph graph, TextureHandle source, Material material, string name)
            {
                TextureDesc descriptor = source.GetDescriptor(graph);
                descriptor.name = name;
                descriptor.msaaSamples = MSAASamples.None;
                descriptor.clearBuffer = false;
                TextureHandle destination = graph.CreateTexture(descriptor);
                graph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(source, destination, material, 0), name);
                return destination;
            }
        }
    }
}
