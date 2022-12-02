// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Common;
    using Core.DataReader.Scn;
    using Data;
    using MetaData;
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
                    // Add collider to block player
                    _meshCollider = sceneGameObject.AddComponent<SceneObjectMeshCollider>();
                }
            }
            return sceneGameObject;
        }

        public override bool IsInteractable(InteractionContext ctx)
        {
            return Activated && ctx.DistanceToActor < MAX_INTERACTION_DISTANCE && ObjectInfo.Times > 0;
        }

        public override IEnumerator Interact(bool triggerredByPlayer)
        {
            if (!IsInteractableBasedOnTimesCount()) yield break;

            ToggleAndSaveSwitchState();

            if (triggerredByPlayer)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new PlayerActorLookAtSceneObjectCommand(ObjectInfo.Id));
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorPerformActionCommand(ActorConstants.PlayerActorVirtualID,
                        ActorConstants.ActionNames[ActorActionType.Check], 1));
            }

            PlaySfxIfAny();

            if (ModelType == SceneObjectModelType.CvdModel)
            {
                yield return GetCvdModelRenderer().PlayOneTimeAnimation(true);

                // Remove collider to allow player to pass through
                if (ObjectInfo.Parameters[0] == 1 && _meshCollider != null)
                {
                    Object.Destroy(_meshCollider);
                }
            }

            ExecuteScriptIfAny();

            yield return ActivateOrInteractWithLinkedObjectIfAny();
        }
    }
}