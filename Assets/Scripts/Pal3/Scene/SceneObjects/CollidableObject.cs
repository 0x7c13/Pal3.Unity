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
        private CollidableObjectController _collidableObjectController;
        
        public CollidableObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }
        
        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            _collidableObjectController = sceneGameObject.AddComponent<CollidableObjectController>();
            _collidableObjectController.Init(this);
            return sceneGameObject;
        }

        public override void Deactivate()
        {
            if (_collidableObjectController != null)
            {
                Object.Destroy(_collidableObjectController);
            }
            
            base.Deactivate();
        }
    }
    
    internal class CollidableObjectController : MonoBehaviour
    {
        private bool _isCollided;
        private CollidableObject _collidableObject;
        private BoxCollider _collider;
        private CvdModelRenderer _cvdModelRenderer;

        public void Init(CollidableObject collidableObject)
        {
            _collidableObject = collidableObject;
        }
        
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
            
            _collidableObject.PlaySfxIfAny();
            
            _cvdModelRenderer.StartOneTimeAnimation(() =>
            {
                _collidableObject.ChangeLinkedObjectGlobalActivationStateIfAny(true);
                _collidableObject.ExecuteScriptIfAny();
            });
            
            Destroy(_collider);
        }
    }
}