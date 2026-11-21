// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser
{
    using UnityEngine;

    // The composite preview owner supplies and disposes optional frame effects.
    // Rendering the scene and presenting its output remain separate operations.
    public interface IResourcePreviewFrameEffect
    {
        Vector2Int TargetSize(int requestedWidth, int requestedHeight);

        Rect Viewport
        {
            get;
        }

        RenderTexture Composite(RenderTexture scene, int requestedWidth, int requestedHeight);
    }
}
