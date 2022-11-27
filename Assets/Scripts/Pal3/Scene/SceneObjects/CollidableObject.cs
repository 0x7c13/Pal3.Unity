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
        private bool _hasCollided;
        private CollidableObject _collidableObject;
        private BoxCollider _collider;
        private CvdModelRenderer _cvdModelRenderer;

        public void Init(CollidableObject collidableObject)
        {
            _collidableObject = collidableObject;
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

                if (_collidableObject.ObjectInfo.IsNonBlocking == 1)
                {
                    _collider.isTrigger = true;   
                }
            }
        }

        private void Interact()
        {
            if (_hasCollided) return;
            _hasCollided = true;
            
            if (!_collidableObject.IsInteractableBasedOnTimesCount()) return;
            
            _collidableObject.PlaySfxIfAny();
            
            _cvdModelRenderer.StartOneTimeAnimation(true, () =>
            {
                _collidableObject.ChangeLinkedObjectActivationStateIfAny(true);
                _collidableObject.ExecuteScriptIfAny();
                SetupCollider(); // reset collider since bounds may change after animation
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