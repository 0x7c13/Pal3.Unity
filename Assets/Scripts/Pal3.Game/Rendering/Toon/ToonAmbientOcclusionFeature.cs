// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Toon
{
    using UnityEngine;
    using UnityEngine.Rendering.Universal;
    using Camera = UnityEngine.Camera;

    public sealed class ToonAmbientOcclusionFeature : ScreenSpaceAmbientOcclusion
    {
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            if (camera.cameraType == CameraType.Game &&
                camera.TryGetComponent(out ToonCameraLighting lighting) &&
                lighting.isActiveAndEnabled && lighting.ContactShadowsEnabled)
            {
                base.AddRenderPasses(renderer, ref renderingData);
            }
        }
    }
}
