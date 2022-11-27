// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Command;
    using Command.SceCommands;
    using Common;
    using Core.DataReader.Scn;
    using Data;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.ElevatorDoor)]
    public class ElevatorDoorObject : SceneObject
    {
        private SceneObjectMeshCollider _meshCollider;
        
        public ElevatorDoorObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }
        
        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            _meshCollider = sceneGameObject.AddComponent<SceneObjectMeshCollider>(); // Add collider to block player
            return sceneGameObject;
        }

        public override void Interact(bool triggerredByPlayer)
        {
            if (!IsInteractableBasedOnTimesCount()) return;
            
            CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxCommand("wg005", 1));
            
            if (ModelType == SceneObjectModelType.CvdModel)
            {
                GetCvdModelRenderer().StartOneTimeAnimation(true, () =>
                {
                    ChangeActivationState(false);
                    SaveActivationState(false);
                });
            }
            else
            {
                ChangeActivationState(false);
                SaveActivationState(false);
            }
        }

        public override void Deactivate()
        {
            if (_meshCollider != null)
            {
                Object.Destroy(_meshCollider);
            }
            
            base.Deactivate();
        }
    }
}