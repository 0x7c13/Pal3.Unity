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
    using Core.Services;
    using Data;
    using MetaData;
    using Player;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.SpecialMechanism)]
    public class SpecialMechanismObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 4f;

        private readonly PlayerManager _playerManager;

        public SpecialMechanismObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _playerManager = ServiceLocator.Instance.Get<PlayerManager>();
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            // Add collider to block player, also make the bounds of the collider a little bit bigger
            // to make sure the player can't walk through the collider
            var boundsScale = (PlayerActorId) ObjectInfo.Parameters[0] switch
            {
                #if PAL3
                PlayerActorId.JingTian => 1.5f,
                PlayerActorId.XueJian => 1.5f,
                PlayerActorId.LongKui => 1.2f,
                PlayerActorId.ZiXuan => 1.2f,
                PlayerActorId.ChangQing => 1.7f,
                #endif
                _ => 1f
            };

            sceneGameObject.AddComponent<SceneObjectMeshCollider>().SetBoundsScale(boundsScale);

            return sceneGameObject;
        }

        public override bool IsInteractable(InteractionContext ctx)
        {
            return Activated && ctx.DistanceToActor < MAX_INTERACTION_DISTANCE;
        }

        public override IEnumerator Interact(bool triggerredByPlayer)
        {
            PlayerActorId actorId = _playerManager.GetPlayerActor();

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
                    ActorConstants.ActionNames[ActorActionType.Skill],
                    1));

            yield return new WaitForSeconds(1.2f); // Wait for actor animation to finish

            #if PAL3
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
                CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxCommand(sfxName, 1));
            }
            #endif

            if (ModelType == SceneObjectModelType.CvdModel)
            {
                yield return GetCvdModelRenderer().PlayOneTimeAnimation(true);
            }

            ChangeAndSaveActivationState(false);
        }
    }
}