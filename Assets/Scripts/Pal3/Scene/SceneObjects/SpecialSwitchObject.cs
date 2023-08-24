// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using Actor.Controllers;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Common;
    using Core.Contracts;
    using Core.DataReader.Scn;
    using Core.Extensions;
    using Data;
    using MetaData;
    using UnityEngine;

    [ScnSceneObject(SceneObjectType.SpecialSwitch)]
    public sealed class SpecialSwitchObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 4f;

        private SceneObjectMeshCollider _meshCollider;

        public SpecialSwitchObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            // Add collider to block player, also make the bounds of the collider a little bit bigger
            // to make sure the player can't walk through the collider
            Vector3 boundsSizeOffset = (PlayerActorId) ObjectInfo.Parameters[0] switch
            {
                PlayerActorId.JingTian => new Vector3(1.5f, 0f, 1.5f),
                PlayerActorId.XueJian => new Vector3(1f, 0f, 1f),
                PlayerActorId.LongKui => new Vector3(2.5f, 0f, 2.5f),
                PlayerActorId.ZiXuan => new Vector3(0.8f, 0f, 0.8f),
                PlayerActorId.ChangQing => new Vector3(2.3f, 0f, 2.3f),
                _ => Vector3.one
            };

            _meshCollider = sceneGameObject.AddComponent<SceneObjectMeshCollider>();
            _meshCollider.Init(boundsSizeOffset);

            return sceneGameObject;
        }

        public override bool IsDirectlyInteractable(float distance)
        {
            return IsActivated && distance < MAX_INTERACTION_DISTANCE;
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            PlayerActorId actorId = (PlayerActorId)ctx.PlayerActorGameObject
                .GetComponent<ActorController>().GetActor().Id;

            if (!Enum.IsDefined(typeof(PlayerActorId), actorId)) yield break;

            // Only specified actor can interact with this object
            if ((int) actorId != ObjectInfo.Parameters[0])
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new UIDisplayNoteCommand("我不能打开这个机关..."));
                yield break;
            }

            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new PlayerActorLookAtSceneObjectCommand(ObjectInfo.Id));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorPerformActionCommand(ActorConstants.PlayerActorVirtualID,
                    ActorConstants.ActionToNameMap[ActorActionType.Skill], 1));

            yield return new WaitForSeconds(1.2f); // Wait for actor animation to finish

            var sfxName = actorId switch
            {
                PlayerActorId.JingTian => "we026",
                PlayerActorId.XueJian => "we027",
                PlayerActorId.LongKui => "we028",
                PlayerActorId.ZiXuan => "we029",
                PlayerActorId.ChangQing => "we030",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(sfxName))
            {
                PlaySfx(sfxName);
            }

            if (ModelType == SceneObjectModelType.CvdModel)
            {
                yield return GetCvdModelRenderer().PlayOneTimeAnimationAsync(true);
            }

            ChangeAndSaveActivationState(false);
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

#endif