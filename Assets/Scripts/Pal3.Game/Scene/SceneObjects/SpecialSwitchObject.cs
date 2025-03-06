// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Game.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using Actor.Controllers;
    using Command;
    using Command.Extensions;
    using Common;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Data;
    using Engine.Core.Abstraction;
    using Engine.Coroutine;
    using Engine.Extensions;

    using Color = Core.Primitives.Color;
    using Vector3 = UnityEngine.Vector3;

    [ScnSceneObject(SceneObjectType.SpecialSwitch)]
    public sealed class SpecialSwitchObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 4f;

        private SceneObjectMeshCollider _meshCollider;

        public SpecialSwitchObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();
            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

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

            _meshCollider = sceneObjectGameEntity.AddComponent<SceneObjectMeshCollider>();
            _meshCollider.Init(boundsSizeOffset);

            return sceneObjectGameEntity;
        }

        public override bool IsDirectlyInteractable(float distance)
        {
            return IsActivated && distance < MAX_INTERACTION_DISTANCE;
        }

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            PlayerActorId actorId = (PlayerActorId)ctx.PlayerActorGameEntity
                .GetComponent<ActorController>().GetActor().Id;

            if (!Enum.IsDefined(typeof(PlayerActorId), actorId)) yield break;

            // Only specified actor can interact with this object
            if ((int) actorId != ObjectInfo.Parameters[0])
            {
                Pal3.Instance.Execute(new UIDisplayNoteCommand("我不能打开这个机关..."));
                yield break;
            }

            Pal3.Instance.Execute(new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
            Pal3.Instance.Execute(new PlayerActorLookAtSceneObjectCommand(ObjectInfo.Id));
            Pal3.Instance.Execute(new ActorPerformActionCommand(ActorConstants.PlayerActorVirtualID,
                    ActorConstants.ActionToNameMap[ActorActionType.Skill], 1));

            yield return CoroutineYieldInstruction.WaitForSeconds(1.2f); // Wait for actor animation to finish

            string sfxName = actorId switch
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