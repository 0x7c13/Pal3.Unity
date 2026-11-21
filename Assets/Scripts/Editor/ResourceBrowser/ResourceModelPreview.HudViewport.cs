// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser
{
    using UnityEngine;

    public sealed partial class ResourceModelPreview
    {
        public bool ExpandReferenceFraming
        {
            get; set;
        }

        public void SetViewportContext(Rect viewport)
        {
            if (viewport.width >= 1 && viewport.height >= 1)
                ConfigureCamera(Mathf.CeilToInt(viewport.width), Mathf.CeilToInt(viewport.height), new Rect(0, 0, 1, 1));
        }

        public float ReferenceProjectionScale(Rect viewport) => !_horizontalFieldOfView.HasValue ? viewport.height / 600 :
            ExpandReferenceFraming ? Mathf.Min(viewport.width / 800, viewport.height / 600) : viewport.width / 800;

        public static float PreviewVerticalFieldOfView(float nativeHorizontalFieldOfView, float aspect, bool expandReference) =>
            Camera.HorizontalToVerticalFieldOfView(nativeHorizontalFieldOfView, expandReference ? Mathf.Min(aspect, 4f / 3) : aspect);
    }
}
