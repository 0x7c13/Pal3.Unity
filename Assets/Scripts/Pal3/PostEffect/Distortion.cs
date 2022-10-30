using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Pal3.posteffect
{
    [Serializable]
    [PostProcess(typeof(DistortionRenderer), PostProcessEvent.AfterStack, "pal3/Distortion")]
    public sealed class Distortion : PostProcessEffectSettings
    {
        [Range(0f, 1f), Tooltip("Grayscale effect intensity.")]
        public FloatParameter blend = new FloatParameter { value = 0.5f };
    }
    public sealed class DistortionRenderer : PostProcessEffectRenderer<Distortion>
    {
        public override void Render(PostProcessRenderContext context)
        {
            var sheet = context.propertySheets.Get(Shader.Find("Pal3/PostEffectDistortion"));
            sheet.properties.SetFloat("_Blend", settings.blend);
            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        }
    }    
}
