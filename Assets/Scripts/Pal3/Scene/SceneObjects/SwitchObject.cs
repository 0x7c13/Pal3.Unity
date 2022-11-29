// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Common;
    using Core.DataReader.Scn;
    using Data;
    using MetaData;
    using State;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.Switch)]
    public class SwitchObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 5f;
        private SceneObjectMeshCollider _meshCollider;
        
        public SwitchObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }
        
        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            if (ObjectInfo.IsNonBlocking == 0)
            {
                if (!(ObjectInfo.SwitchState == 1 && ObjectInfo.Parameters[0] == 1))
                {
                    _meshCollider = sceneGameObject.AddComponent<SceneObjectMeshCollider>(); // Add collider to block player
                }
            }
            return sceneGameObject;
        }

        public override bool IsInteractable(InteractionContext ctx)
        {
            return Activated && ctx.DistanceToActor < MAX_INTERACTION_DISTANCE && ObjectInfo.Times > 0;
        }

        public override void Interact(bool triggerredByPlayer)
        {
            if (!IsInteractableBasedOnTimesCount()) return;

            ToggleSwitchState();
            
            if (triggerredByPlayer)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new GameStateChangeRequest(GameState.Cutscene));
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new PlayerActorLookAtSceneObjectCommand(ObjectInfo.Id));
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorPerformActionCommand(ActorConstants.PlayerActorVirtualID,
                        ActorConstants.ActionNames[ActorActionType.Check],
                        1));
            }

            if (ModelType == SceneObjectModelType.CvdModel)
            {
                PlaySfxIfAny();
                
                GetCvdModelRenderer().StartOneTimeAnimation(true, () =>
                {
                    // Remove collider to allow player to pass through
                    if (ObjectInfo.Parameters[0] == 1 && _meshCollider != null)
                    {
                        Object.Destroy(_meshCollider);
                    }

                    InteractInternal();
                });
            }
            else
            {
                InteractInternal();
            }
        }

        private void InteractInternal()
        {
            if (!InteractWithLinkedObjectIfAny() && !ExecuteScriptIfAny())
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new GameStateChangeRequest(GameState.Gameplay));
            }
        }
    }
}