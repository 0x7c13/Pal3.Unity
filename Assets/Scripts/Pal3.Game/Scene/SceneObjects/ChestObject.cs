// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Command.Extensions;
    using Common;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Core.Primitives;
    using Data;
    using Engine.Core.Abstraction;
    using Engine.Extensions;

    [ScnSceneObject(SceneObjectType.Chest)]
    public sealed class ChestObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 3f;

        private SceneObjectMeshCollider _meshCollider;

        public ChestObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override bool IsDirectlyInteractable(float distance)
        {
            return IsActivated && distance < MAX_INTERACTION_DISTANCE;
        }

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();
            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);
            _meshCollider = sceneObjectGameEntity.AddComponent<SceneObjectMeshCollider>();
            return sceneObjectGameEntity;
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            if (!IsInteractableBasedOnTimesCount()) yield break;

            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new PlayerActorLookAtSceneObjectCommand(ObjectInfo.Id));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorPerformActionCommand(ActorConstants.PlayerActorVirtualID,
                    ActorConstants.ActionToNameMap[ActorActionType.Check], 1));

            PlaySfx("wg011");

            if (ModelType == SceneObjectModelType.CvdModel)
            {
                yield return GetCvdModelRenderer().PlayOneTimeAnimationAsync(true);
                ExecuteScriptIfAny();
                ChangeAndSaveActivationState(false);
            }
            else
            {
                ExecuteScriptIfAny();
                ChangeAndSaveActivationState(false);
            }

            for (int i = 0; i < 4; i++)
            {
                if (ObjectInfo.Parameters[i] != 0)
                {
                    CommandDispatcher<ICommand>.Instance.Dispatch(new InventoryAddItemCommand(ObjectInfo.Parameters[i], 1));
                }
            }

            #if PAL3A
            if (ObjectInfo.Parameters[5] != 0) // money
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new InventoryAddMoneyCommand(ObjectInfo.Parameters[5]));
            }
            #endif

            PlaySfx("wa006");
        }

        public override void Deactivate()
        {
            if (_meshCollider != null)
            {
                _meshCollider.Destroy();
                _meshCollider = null;
            }

            base.Deactivate();
        }
    }
}