// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System;
    using Common;
    using Core.DataReader.Scn;
    using Data;
    using Renderer;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [ScnSceneObject(ScnSceneObjectType.Collidable)]
    [ScnSceneObject(ScnSceneObjectType.Shakeable)]
    public class CollidableObject : SceneObject
    {
        private CollidableObjectController _objectController;
        
        public CollidableObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }
        
        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            _objectController = sceneGameObject.AddComponent<CollidableObjectController>();
            _objectController.Init(this);
            return sceneGameObject;
        }

        public override void Deactivate()
        {
            if (_objectController != null)
            {
                Object.Destroy(_objectController);
            }
            
            base.Deactivate();
        }
    }
    
    internal class CollidableObjectController : MonoBehaviour
    {
        private bool _hasCollided;
        private CollidableObject _object;
        private BoxCollider _collider;
        private CvdModelRenderer _cvdModelRenderer;

        public void Init(CollidableObject collidableObject)
        {
            _object = collidableObject;
            _cvdModelRenderer = gameObject.GetComponent<CvdModelRenderer>();
            SetupCollider();
        }

        private void SetupCollider()
        {
            if (_cvdModelRenderer != null)
            {
                Bounds bounds = _cvdModelRenderer.GetMeshBounds();

                if (_collider == null)
                {
                    _collider = gameObject.AddComponent<BoxCollider>();
                }

                _collider.center = bounds.center;
                _collider.size = bounds.size;

                if (_object.ObjectInfo.IsNonBlocking == 1)
                {
                    _collider.isTrigger = true;   
                }
            }
        }

        private void Interact()
        {
            if (_hasCollided) return;
            _hasCollided = true;
            
            if (!_object.IsInteractableBasedOnTimesCount()) return;
            
            _object.PlaySfxIfAny();
            
            _cvdModelRenderer.StartOneTimeAnimation(true, () =>
            {
                _object.ChangeLinkedObjectActivationStateIfAny(true);
                _object.ExecuteScriptIfAny();
                SetupCollider(); // Reset collider since bounds may change after animation
            });
        }
        
        private void OnDisable()
        {
            if (_collider != null)
            {
                Destroy(_collider);   
            }
        }

        private void OnCollisionEnter(Collision _)
        {
            Interact();
        }

        private void OnTriggerEnter(Collider _)
        {
            Interact();
        }
    }
}