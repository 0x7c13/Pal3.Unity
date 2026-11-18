// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser
{
    using System;
    using Core.DataReader.Cvd;
    using Game.Rendering.Renderer;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public sealed partial class ResourceModelPreview
    {
        // Composite models share this owner's scene, materials and texture cache.
        // No camera framing or stand-alone resource animation is performed here.
        public CvdModelRenderer SetHierarchicalModel(GameObject target, string resourcePath, CvdFile model)
        {
            if (Root == null)
                throw new ObjectDisposedException(nameof(ResourceModelPreview));
            if (target == null || !target.transform.IsChildOf(Root.transform))
                throw new ArgumentException("The model must belong to this preview.", nameof(target));
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            CvdModelRenderer previous = target.GetComponentInChildren<CvdModelRenderer>();
            GameObject geometry = NewObject("Hierarchical geometry", target.transform);
            var renderer = geometry.AddComponent<CvdModelRenderer>();
            try
            {
                renderer.Init(model, new ModelTextures(this, DirectoryOf(resourcePath)), _materials);
                renderer.SetCurrentTime(0);
            }
            catch
            {
                renderer.Dispose();
                Object.DestroyImmediate(geometry);
                throw;
            }
            if (previous != null)
            {
                _renderers.Remove(previous);
                previous.Dispose();
                Object.DestroyImmediate(previous.gameObject);
            }
            _renderers.Add(renderer);
            foreach (Transform child in geometry.GetComponentsInChildren<Transform>(true))
                child.gameObject.hideFlags = HideFlags.HideAndDontSave;
            return renderer;
        }
    }
}
