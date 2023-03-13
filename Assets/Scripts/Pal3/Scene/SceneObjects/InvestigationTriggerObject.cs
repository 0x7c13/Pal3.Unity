// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Command.SceCommands;
    using Common;
    using Core.DataReader.Scn;
    using Data;
    using MetaData;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.VirtualInvestigationTrigger)]
    [ScnSceneObject(ScnSceneObjectType.InvestigationTrigger)]
    public sealed class InvestigationTriggerObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 4f;

        public InvestigationTriggerObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            if (ObjectInfo.IsNonBlocking == 0)
            {
                sceneGameObject.AddComponent<SceneObjectMeshCollider>(); // Add collider to block player
            }
            return sceneGameObject;
        }

        public override bool IsDirectlyInteractable(float distance)
        {
            return Activated && distance < MAX_INTERACTION_DISTANCE;
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            if (!IsInteractableBasedOnTimesCount()) yield break;

            if (ctx.StartedByPlayer && ctx.InitObjectId == ObjectInfo.Id)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
            }

            #if PAL3A
            if (ObjectInfo.Parameters[0] == 1)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorPerformActionCommand(ActorConstants.PlayerActorVirtualID,
                        ActorConstants.ActionNames[ActorActionType.Check], 1));
                yield return new WaitForSeconds(1);
            }
            #endif

            yield return ExecuteScriptAndWaitForFinishIfAnyAsync();
        }
    }
}