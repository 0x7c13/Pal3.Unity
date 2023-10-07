// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene.SceneObjects.Common
{
    using Engine.Abstraction;
    using Engine.Extensions;
    using Rendering.Renderer;
    using UnityEngine;

    public class SceneObjectMeshCollider : GameEntityScript
    {
        private BoxCollider _collider;
        private Vector3 _meshBoundsSize;
        private Vector3 _boundsSizeOffset;

        private bool _initialized;

        protected override void OnEnableGameEntity()
        {
            if (!_initialized)
            {
                UpdateBounds();
            }
        }

        protected override void OnDisableGameEntity()
        {
            if (_collider != null)
            {
                _collider.Destroy();
                _collider = null;
            }
        }

        /// <summary>
        /// Init with a size offset.
        /// Note: This method is not required to be called.
        /// </summary>
        /// <param name="sizeOffset">A size offset to the bounds based on model</param>
        public void Init(Vector3 sizeOffset)
        {
            _boundsSizeOffset = sizeOffset;
            UpdateBounds();
            _initialized = true;
        }

        private void UpdateBounds()
        {
            Bounds bounds;

            if (GameEntity.GetComponent<CvdModelRenderer>() is { } cvdModelRenderer)
            {
                bounds = cvdModelRenderer.GetMeshBounds();
            }
            else if (GameEntity.GetComponent<PolyModelRenderer>() is { } polyModelRenderer)
            {
                bounds = polyModelRenderer.GetMeshBounds();
            }
            else
            {
                return;
            }

            if (_collider == null)
            {
                _collider = GameEntity.AddComponent<BoxCollider>();
            }

            _meshBoundsSize = bounds.size;

            _collider.center = bounds.center;
            _collider.size = _meshBoundsSize + _boundsSizeOffset;
        }
    }
}