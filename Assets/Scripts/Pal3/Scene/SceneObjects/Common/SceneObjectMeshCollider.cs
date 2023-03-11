﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects.Common
{
    using Renderer;
    using UnityEngine;

    public class SceneObjectMeshCollider : MonoBehaviour
    {
        private BoxCollider _collider;
        private Vector3 _meshBoundsSize;
        private Vector3 _boundsSizeOffset;

        public void SetBoundsSizeOffset(Vector3 sizeOffset)
        {
            _boundsSizeOffset = sizeOffset;
            UpdateBounds();
        }

        private void Start()
        {
            UpdateBounds();
        }

        private void UpdateBounds()
        {
            Bounds bounds;

            if (GetComponent<CvdModelRenderer>() is { } cvdModelRenderer)
            {
                bounds = cvdModelRenderer.GetMeshBounds();
            }
            else if (GetComponent<PolyModelRenderer>() is { } polyModelRenderer)
            {
                bounds = polyModelRenderer.GetMeshBounds();
            }
            else
            {
                return;
            }

            if (_collider == null)
            {
                _collider = gameObject.AddComponent<BoxCollider>();
            }

            _meshBoundsSize = bounds.size;

            _collider.center = bounds.center;
            _collider.size = _meshBoundsSize + _boundsSizeOffset;
        }

        private void OnDisable()
        {
            if (_collider != null)
            {
                Destroy(_collider);
            }
        }
    }
}