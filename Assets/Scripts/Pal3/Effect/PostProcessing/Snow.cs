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
    [PostProcess(typeof(SnowRenderer), PostProcessEvent.AfterStack, "Pal3/Snow")]
    public sealed class Snow : PostProcessEffectSettings
    {
        // Blizzard
        // #define LAYERS 200
        // #define DEPTH .1
        // #define WIDTH .8
        // #define SPEED 1.5
        
        // Light Snow
        // _Layers ("Layers",int) = 50
        // _Depth ("Depth",float) = 0.5
        // _Width("Width",float) = 0.3
        // _Speed("Speed",float) = 0.6
        
        [Range(0,200),Tooltip("Layers")]
        public IntParameter Layers = new() { value = 50 };

        [Range(0.0f, 1.0f), Tooltip("Depth")] 
        public FloatParameter Depth = new() { value = 0.5f };
        
        [Range(0.0f, 1.0f), Tooltip("Width")] 
        public FloatParameter Width = new() { value = 0.3f };

        [Range(0.0f, 2.0f), Tooltip("Speed")] 
        public FloatParameter Speed = new() { value = 0.6f };

    }

    public sealed class SnowRenderer : PostProcessEffectRenderer<Snow>
    {
        private static readonly int LayersPropertyId = Shader.PropertyToID("_Layers");
        private static readonly int DepthPropertyId = Shader.PropertyToID("_Depth");
        private static readonly int WidthPropertyId = Shader.PropertyToID("_Width");
        private static readonly int SpeedPropertyId = Shader.PropertyToID("_Speed");
        
        private Shader _shader;

        public override void Init()
        {
            _shader = Shader.Find("Pal3/PostEffectSnow");
            base.Init();
        }

        public override void Render(PostProcessRenderContext context)
        {
            PropertySheet sheet = context.propertySheets.Get(_shader);
            
            sheet.properties.SetFloat(LayersPropertyId, settings.Layers);
            sheet.properties.SetFloat(DepthPropertyId, settings.Depth);
            sheet.properties.SetFloat(WidthPropertyId, settings.Width);
            sheet.properties.SetFloat(SpeedPropertyId, settings.Speed);
            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        }
    }
}
