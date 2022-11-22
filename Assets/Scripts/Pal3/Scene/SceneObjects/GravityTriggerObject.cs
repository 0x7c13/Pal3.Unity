// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Common;
    using Core.DataReader.Scn;
    using Data;
    using Renderer;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.GravityTrigger)]
    public class GravityTriggerObject : SceneObject
    {
        private StandingPlatformController _platformController;
        private GravityTriggerObjectController _gravityTriggerObjectController;
        
        public GravityTriggerObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }
        
        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            
            var cvdModelRenderer = sceneGameObject.GetComponent<CvdModelRenderer>();

            Bounds triggerBounds = cvdModelRenderer.GetMeshBounds();
            Bounds rendererBounds = cvdModelRenderer.GetRendererBounds();
            
            // This is a tweak based on the model
            {
                rendererBounds.size *= 0.7f;
                rendererBounds.center = new Vector3(
                    rendererBounds.center.x,
                    rendererBounds.center.y + 0.2f,
                    rendererBounds.center.z);
            }
            
            _platformController = sceneGameObject.AddComponent<StandingPlatformController>();
            _platformController.SetBounds(triggerBounds, rendererBounds);
            
            _gravityTriggerObjectController = sceneGameObject.AddComponent<GravityTriggerObjectController>();
            _gravityTriggerObjectController.Init(this);
            
            return sceneGameObject;
        }

        public override void Deactivate()
        {
            if (_platformController != null)
            {
                Object.Destroy(_platformController);
            }
            
            if (_gravityTriggerObjectController != null)
            {
                Object.Destroy(_gravityTriggerObjectController);
            }
            
            base.Deactivate();
        }
    }

    public class GravityTriggerObjectController : MonoBehaviour
    {
        private GravityTriggerObject _gravityTriggerObject;
        
        public void Init(GravityTriggerObject gravityTriggerObject)
        {
            _gravityTriggerObject = gravityTriggerObject;
        }
    }
}