// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser
{
    using System;
    using System.Collections.Generic;
    using Core.DataReader.Pol;
    using Engine.Core.Implementation;
    using Game.Effect.Runtime;
    using Game.Rendering.Renderer;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public sealed partial class ResourceModelPreview
    {
        private GameObject _environment;

        public PolyModelRenderer AddPolyModel(GameObject target, string resourcePath, PolFile model, string texture = null)
        {
            if (target == null || !target.transform.IsChildOf(Root.transform))
                throw new ArgumentException("The model must belong to this preview.", nameof(target));
            var renderer = target.AddComponent<PolyModelRenderer>();
            try
            {
                renderer.Render(model, new ModelTextures(this, DirectoryOf(resourcePath), texture), _materials,
                    isStaticObject: false, textureFactory: new UnityTextureFactory(), animateWater: false);
                _renderers.Add(renderer);
                return renderer;
            }
            catch
            {
                renderer.Dispose();
                Object.DestroyImmediate(renderer);
                throw;
            }
        }

        public void ReplaceEnvironment(ResourceEntry geometry, PolFile model, Func<int, int, PolSurfaceAppearance?> surfaceAppearance = null)
        {
            if (geometry == null || model == null)
                throw new ArgumentException("A preview environment requires its original geometry and resource paths.");
            GameObject candidate = NewObject("Battle environment " + geometry.Name, Root.transform);
            var renderers = new List<IDisposable>();
            try
            {
                var poly = candidate.AddComponent<PolyModelRenderer>();
                renderers.Add(poly);
                poly.Render(model, new ModelTextures(this, DirectoryOf(geometry.Path)), _materials,
                    isStaticObject: true, textureFactory: new UnityTextureFactory(), animateWater: false, surfaceAppearance: surfaceAppearance);
            }
            catch
            {
                foreach (IDisposable renderer in renderers)
                    renderer.Dispose();
                Object.DestroyImmediate(candidate);
                throw;
            }
            if (_environment != null)
                ReleaseCompositeObject(_environment);
            _environment = candidate;
            _renderers.AddRange(renderers);
        }

        public void SetEnvironmentRenderOrder(int order)
        {
            if (_environment == null)
                return;
            foreach (Renderer renderer in _environment.GetComponentsInChildren<Renderer>(true))
                renderer.sortingOrder = order;
        }

        // Composite objects have no entries in the stand-alone resource sampler.
        // Their renderers return materials to the shared owner and release meshes.
        public void ReleaseCompositeObject(GameObject target)
        {
            if (target == null || !target.transform.IsChildOf(Root.transform))
                throw new ArgumentException("The object must belong to this composite preview.", nameof(target));
            DisposeEffects(target);
            foreach (MonoBehaviour component in target.GetComponentsInChildren<MonoBehaviour>(true))
                if (component is IDisposable renderer && _renderers.Remove(renderer))
                    renderer.Dispose();
            Object.DestroyImmediate(target);
        }

        private static void DisposeEffects(GameObject root)
        {
            foreach (EffectPlayer player in root.GetComponentsInChildren<EffectPlayer>(true))
                player.Dispose();
            foreach (PortalEffectPlayer portal in root.GetComponentsInChildren<PortalEffectPlayer>(true))
                portal.Dispose();
        }
    }
}
