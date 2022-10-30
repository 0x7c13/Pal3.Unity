using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Pal3.posteffect
{
    [Serializable]
    [PostProcess(typeof(DistortionRenderer), PostProcessEvent.AfterStack, "pal3/Distortion")]
    public sealed class Distortion : PostProcessEffectSettings
    {
        [Range(0.1f, 30.0f), Tooltip("TimeScale")]
        public FloatParameter timeScale = new FloatParameter { value = 4.0f };
           
        [Range(0.1f, 30.0f), Tooltip("X Factor")]
        public FloatParameter xFactor = new FloatParameter { value = 15.0f };
        
        [Range(0.1f, 30.0f), Tooltip("Y Factor")]
        public FloatParameter yFactor = new FloatParameter { value = 10.0f };
        
    }
    public sealed class DistortionRenderer : PostProcessEffectRenderer<Distortion>
    {
        public override void Render(PostProcessRenderContext context)
        {
            var sheet = context.propertySheets.Get(Shader.Find("Pal3/PostEffectDistortion"));
            sheet.properties.SetFloat("_TimeScale", settings.timeScale);
            sheet.properties.SetFloat("_XFactor", settings.xFactor);
            sheet.properties.SetFloat("_YFactor", settings.yFactor);
            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        }
    }    
}
