// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Effect.PostProcessing
{
    using System;
    using Engine.Services;
    using UnityEngine;
    using UnityEngine.Rendering.PostProcessing;

    [Serializable]
    [PostProcess(typeof(DistortionRenderer), PostProcessEvent.AfterStack, "Pal3/Distortion")]
    public sealed class Distortion : PostProcessEffectSettings
    {
        [Range(0.1f, 30.0f), Tooltip("TimeScale")]
        public FloatParameter timeScale = new() { value = 4.0f };

        [Range(0.1f, 30.0f), Tooltip("X Factor")]
        public FloatParameter xFactor = new() { value = 15.0f };

        [Range(0.1f, 30.0f), Tooltip("Y Factor")]
        public FloatParameter yFactor = new() { value = 10.0f };
    }

    public sealed class DistortionRenderer : PostProcessEffectRenderer<Distortion>
    {
        private static readonly int TimeScalePropertyId = ShaderUtility.GetPropertyIdByName("_TimeScale");
        private static readonly int XFactorPropertyId = ShaderUtility.GetPropertyIdByName("_XFactor");
        private static readonly int YFactorPropertyId = ShaderUtility.GetPropertyIdByName("_YFactor");

        private Shader _distortionShader;

        public override void Init()
        {
            _distortionShader = Shader.Find("Pal3/PostEffectDistortion");
            base.Init();
        }

        public override void Render(PostProcessRenderContext context)
        {
            PropertySheet sheet = context.propertySheets.Get(_distortionShader);
            sheet.properties.SetFloat(TimeScalePropertyId, settings.timeScale);
            sheet.properties.SetFloat(XFactorPropertyId, settings.xFactor);
            sheet.properties.SetFloat(YFactorPropertyId, settings.yFactor);
            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        }
    }
}
