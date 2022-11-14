// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System;
    using Core.DataReader.Scn;
    using Core.Utils;
    using Data;
    using Renderer;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.Collidable)]
    public class CollidableObject : SceneObject
    {
        public CollidableObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }
        
        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            sceneGameObject.AddComponent<CollidableObjectController>();
            return sceneGameObject;
        }
    }
    
    internal class CollidableObjectController : MonoBehaviour
    {
        private bool _isCollided;
        private BoxCollider _collider;
        private CvdModelRenderer _cvdModelRenderer;

        private void OnEnable()
        {
            _cvdModelRenderer = gameObject.GetComponent<CvdModelRenderer>();
            if (_cvdModelRenderer != null)
            {
                _collider = gameObject.AddComponent<BoxCollider>();
                Bounds bounds = gameObject.GetComponent<CvdModelRenderer>().GetMeshBounds();
                _collider.center = bounds.center;
                _collider.size = bounds.size;
                _collider.isTrigger = true;
            }
        }

        private void OnDisable()
        {
            if (_collider != null)
            {
                Destroy(_collider);   
            }
        }

        private void OnTriggerEnter(Collider _)
        {
            if (_isCollided) return;
            
            _isCollided = true;
            
            _cvdModelRenderer.PlayAnimation(timeScale: 1, loopCount: 1);
            
            Destroy(_collider);
        }
    }
}