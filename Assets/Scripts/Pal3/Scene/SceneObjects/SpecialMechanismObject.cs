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
        private const float MAX_INTERACTION_DISTANCE = 5f;

        private SpecialMechanismObjectController _specialMechanismObjectController;
        
        public SpecialMechanismObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            sceneGameObject.AddComponent<SceneObjectMeshCollider>(); // Add collider to block player
            _specialMechanismObjectController = sceneGameObject.AddComponent<SpecialMechanismObjectController>();
            _specialMechanismObjectController.Init(this);
            return sceneGameObject;
        }

        public override bool IsInteractable(InteractionContext ctx)
        {
            return Activated &&
                   ctx.DistanceToActor < MAX_INTERACTION_DISTANCE &&
                   ctx.ActorId == ObjectInfo.Parameters[0]; // Only specified actor can interact with this object
        }

        public override void Interact(bool triggerredByPlayer)
        {
            if (_specialMechanismObjectController != null)
            {
                _specialMechanismObjectController.Interact();
            }
        }

        public override void Deactivate()
        {
            if (_specialMechanismObjectController != null)
            {
                Object.Destroy(_specialMechanismObjectController);
            }
            
            base.Deactivate();
        }
    }

    internal class SpecialMechanismObjectController : MonoBehaviour
    {
        private PlayerManager _playerManager;
        private SpecialMechanismObject _specialMechanismObject;
        
        public void Init(SpecialMechanismObject specialMechanismObject)
        {
            _specialMechanismObject = specialMechanismObject;
            _playerManager = ServiceLocator.Instance.Get<PlayerManager>();
        }

        public void Interact()
        {
            StartCoroutine(InteractInternal());
        }

        private IEnumerator InteractInternal()
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new PlayerEnableInputCommand(0));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new PlayerActorLookAtSceneObjectCommand(_specialMechanismObject.ObjectInfo.Id));
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
            
            if (_specialMechanismObject.ModelType == SceneObjectModelType.CvdModel)
            {
                yield return _specialMechanismObject.GetCvdModelRenderer().PlayOneTimeAnimation();
                FinishingSteps();
            }
            else
            {
                FinishingSteps();
            }
        }

        private void FinishingSteps()
        {
            _specialMechanismObject.ChangeActivationState(false);
            _specialMechanismObject.SaveActivationState(false);
            CommandDispatcher<ICommand>.Instance.Dispatch(new PlayerEnableInputCommand(1));
        }
    }
}