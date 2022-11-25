// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Core.DataReader.Scn;
    using Data;
    using Renderer;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.Collidable)]
    [ScnSceneObject(ScnSceneObjectType.Shakeable)]
    public class CollidableObject : SceneObject
    {
        public CollidableObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }
        
        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
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
                Bounds bounds = _cvdModelRenderer.GetMeshBounds();
                _collider = gameObject.AddComponent<BoxCollider>();
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
            
            _cvdModelRenderer.StartOneTimeAnimation();
            
            Destroy(_collider);
        }
    }
}