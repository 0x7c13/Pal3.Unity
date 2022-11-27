// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
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
        private float _boundsScale = 1.0f;

        public void SetBoundsScale(float boundsScale)
        {
            _boundsScale = boundsScale;
            
            if (_collider != null)
            {
                _collider.size = _meshBoundsSize * _boundsScale;
            }
        }
        
        private void Start()
        {
            UpdateBounds();
        }

        public void UpdateBounds()
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
            _collider.size = _meshBoundsSize * _boundsScale;
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