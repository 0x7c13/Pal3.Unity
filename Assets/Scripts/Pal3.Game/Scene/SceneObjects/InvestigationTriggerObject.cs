// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Common;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Data;
    using Engine.Abstraction;
    using UnityEngine;
    using Color = Core.Primitives.Color;

    [ScnSceneObject(SceneObjectType.VirtualInvestigationTrigger)]
    [ScnSceneObject(SceneObjectType.InvestigationTrigger)]
    public sealed class InvestigationTriggerObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 4f;

        public InvestigationTriggerObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();
            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);
            if (ObjectInfo.IsNonBlocking == 0)
            {
                sceneObjectGameEntity.AddComponent<SceneObjectMeshCollider>(); // Add collider to block player
            }
            return sceneObjectGameEntity;
        }

        public override bool IsDirectlyInteractable(float distance)
        {
            return IsActivated && distance < MAX_INTERACTION_DISTANCE;
        }

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

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
                        ActorConstants.ActionToNameMap[ActorActionType.Check], 1));
                yield return new WaitForSeconds(1);
            }
            #endif

            yield return ExecuteScriptAndWaitForFinishIfAnyAsync();
        }
    }
}