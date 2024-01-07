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
    [PostProcess(typeof(SnowEffectRenderer), PostProcessEvent.AfterStack, "Pal3/Snow")]
    public sealed class Snow : PostProcessEffectSettings
    {
        [Range(0.1f, 5.0f), Tooltip("Snow falling speed [X axis]")]
        public FloatParameter xSpeed = new() { value = 2.0f };

        [Range(0.1f, 5.0f), Tooltip("Snow falling speed [Y axis]")]
        public FloatParameter ySpeed = new() { value = 1.0f };
    }

    public sealed class SnowEffectRenderer : PostProcessEffectRenderer<Snow>
    {
        private static readonly int XSpeedPropertyId = ShaderUtility.GetPropertyIdByName("_xSpeed");
        private static readonly int YSpeedPropertyId = ShaderUtility.GetPropertyIdByName("_ySpeed");

        private Shader _shader;

        public override void Init()
        {
            _shader = Shader.Find("Pal3/Snow");
            base.Init();
        }

        public override void Render(PostProcessRenderContext context)
        {
            PropertySheet sheet = context.propertySheets.Get(_shader);
            sheet.properties.SetFloat(XSpeedPropertyId, settings.xSpeed);
            sheet.properties.SetFloat(YSpeedPropertyId, settings.ySpeed);
            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        }
    }
}