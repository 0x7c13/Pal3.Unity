// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Settings
{
    using System;
    using UnityEngine.Rendering.Universal;

    // Game and ResourceViewer apply the saved setting during their respective scenes.
    public sealed class MultisampleAntiAliasing : IDisposable
    {
        private readonly UniversalRenderPipelineAsset _pipeline;
        private readonly int _originalSampleCount;
        private bool _disposed;

        public MultisampleAntiAliasing(int samples)
        {
            _pipeline = UniversalRenderPipeline.asset;
            _originalSampleCount = _pipeline.msaaSampleCount;
            Apply(samples);
        }

        public void Apply(int samples)
        {
            _pipeline.msaaSampleCount = samples is 2 or 4 or 8 ? samples : 1;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_pipeline != null) _pipeline.msaaSampleCount = _originalSampleCount;
        }
    }
}
