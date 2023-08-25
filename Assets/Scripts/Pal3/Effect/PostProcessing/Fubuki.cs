// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

using Core.Extensions;

namespace Pal3.Effect.PostProcessing
{
    using System;
    using UnityEngine;
    using UnityEngine.Rendering.PostProcessing;

    [Serializable]
    [PostProcess(typeof(FubukiRenderer), PostProcessEvent.AfterStack, "Pal3/Fubuki")]
    public sealed class Fubuki : PostProcessEffectSettings
    {

    }

    public sealed class FubukiRenderer : PostProcessEffectRenderer<Fubuki>
    {
        private Shader _shader;

        public override void Init()
        {
            _shader = Shader.Find("Pal3/PostEffectFubuki");
            base.Init();
        }

        public override void Render(PostProcessRenderContext context)
        {
            PropertySheet sheet = context.propertySheets.Get(_shader);
            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        }
    }
}
