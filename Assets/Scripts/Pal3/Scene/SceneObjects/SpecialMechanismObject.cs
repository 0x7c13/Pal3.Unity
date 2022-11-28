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
        private SpecialMechanismObjectController _objectController;
        
        public SpecialMechanismObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            
            // Add collider to block player, also make the bounds of the collider a little bit bigger
            // to make sure the player can't walk through the collider
            var boundsScale = (PlayerActorId)ObjectInfo.Parameters[0] switch
            {
                #if PAL3
                PlayerActorId.JingTian => 1.5f,
                PlayerActorId.XueJian => 1.5f,
                PlayerActorId.LongKui => 1.2f,
                PlayerActorId.ZiXuan => 1.3f,
                PlayerActorId.ChangQing => 1.7f,
                #endif
                _ => 1f
            };
            
            sceneGameObject.AddComponent<SceneObjectMeshCollider>().SetBoundsScale(boundsScale);
            
            _objectController = sceneGameObject.AddComponent<SpecialMechanismObjectController>();
            _objectController.Init(this);
            
            return sceneGameObject;
        }

        private float GetInteractionMaxDistance()
        {
            return (PlayerActorId) ObjectInfo.Parameters[0] switch
            {
                #if PAL3
                PlayerActorId.JingTian => 4.5f,
                PlayerActorId.XueJian => 5f,
                PlayerActorId.LongKui => 8f,
                PlayerActorId.ZiXuan  => 4.5f, 
                PlayerActorId.ChangQing => 4.5f,
                #endif
                _ => 4.5f
            };
        }

        public override bool IsInteractable(InteractionContext ctx)
        {
            return Activated &&
                   ctx.DistanceToActor < GetInteractionMaxDistance() &&
                   ctx.ActorId == ObjectInfo.Parameters[0]; // Only specified actor can interact with this object
        }

        public override void Interact(bool triggerredByPlayer)
        {
            if (_objectController != null)
            {
                _objectController.Interact();
            }
        }

        public override void Deactivate()
        {
            if (_objectController != null)
            {
                Object.Destroy(_objectController);
            }
            
            base.Deactivate();
        }
    }

    internal class SpecialMechanismObjectController : MonoBehaviour
    {
        private PlayerManager _playerManager;
        private SpecialMechanismObject _object;
        
        public void Init(SpecialMechanismObject specialMechanismObject)
        {
            _object = specialMechanismObject;
            _playerManager = ServiceLocator.Instance.Get<PlayerManager>();
        }

        public void Interact()
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new PlayerEnableInputCommand(0));
            StartCoroutine(InteractInternal());
        }

        private IEnumerator InteractInternal()
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new PlayerActorLookAtSceneObjectCommand(_object.ObjectInfo.Id));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorPerformActionCommand(ActorConstants.PlayerActorVirtualID,
                    ActorConstants.ActionNames[ActorActionType.Skill],
                    1));

            yield return new WaitForSeconds(1.2f); // Wait for actor animation to finish

            PlayerActorId actorId = _playerManager.GetPlayerActor();

            #if PAL3
            var sfxName = actorId switch
            {
                PlayerActorId.JingTian   => "we026",
                PlayerActorId.XueJian    => "we027",
                PlayerActorId.LongKui    => "we028",
                PlayerActorId.ZiXuan     => "we029",
                PlayerActorId.ChangQing  => "we030",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(sfxName))
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxCommand(sfxName, 1));
            }
            #endif
            
            if (_object.ModelType == SceneObjectModelType.CvdModel)
            {
                yield return _object.GetCvdModelRenderer().PlayOneTimeAnimation(true);
                FinishingSteps();
            }
            else
            {
                FinishingSteps();
            }
        }

        private void FinishingSteps()
        {
            _object.ChangeActivationState(false);
            CommandDispatcher<ICommand>.Instance.Dispatch(new PlayerEnableInputCommand(1));
        }
    }
}