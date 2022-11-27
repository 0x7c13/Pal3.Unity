// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Actor;
    using Command;
    using Command.SceCommands;
    using Common;
    using Core.Animation;
    using Core.DataReader.Scn;
    using Core.Services;
    using Data;
    using Player;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.GravityTrigger)]
    public class GravityTriggerObject : SceneObject
    {
        public const float DescendingHeight = 0.5f;
        public const float DescendingAnimationDuration = 2.5f;
        
        private StandingPlatformController _platformController;
        private GravityTriggerObjectController _gravityTriggerObjectController;
        
        private readonly PlayerManager _playerManager;
        private readonly TeamManager _teamManager;
        
        public GravityTriggerObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _playerManager = ServiceLocator.Instance.Get<PlayerManager>();
            _teamManager = ServiceLocator.Instance.Get<TeamManager>();
        }
        
        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            
            var bounds = new Bounds
            {
                center = new Vector3(0f, 0.8f, 0f),
                size = new Vector3(4f, 1f, 4f),
            };
            
            _platformController = sceneGameObject.AddComponent<StandingPlatformController>();
            _platformController.SetBounds(bounds, ObjectInfo.LayerIndex);
            _platformController.OnTriggerEntered += OnPlatformTriggerEntered;
            
            _gravityTriggerObjectController = sceneGameObject.AddComponent<GravityTriggerObjectController>();
            _gravityTriggerObjectController.Init(this);

            // Set to final position if the gravity trigger is already activated
            if (ObjectInfo.Times == 0)
            {
                Vector3 finalPosition = sceneGameObject.transform.position;
                finalPosition.y -= DescendingHeight;
                sceneGameObject.transform.position = finalPosition;
            }
            
            return sceneGameObject;
        }

        private void OnPlatformTriggerEntered(object sender, Collider collider)
        {
            if (!IsInteractableBasedOnTimesCount()) return;
            
            // Check if the player actor is on the platform
            if (collider.gameObject.GetComponent<ActorController>() is {} actorController &&
                actorController.GetActor().Info.Id == (byte)_playerManager.GetPlayerActor())
            {
                // Check if total team members are equal to or greater than required headcount
                if (_teamManager.GetActorsInTeam().Count >= ObjectInfo.Parameters[0])
                {
                    _gravityTriggerObjectController.Interact(collider.gameObject);
                }
                else
                {
                    CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand("重量不足，无法激活"));
                }
            }
        }

        public override void Deactivate()
        {
            if (_platformController != null)
            {
                _platformController.OnTriggerEntered -= OnPlatformTriggerEntered;
                Object.Destroy(_platformController);
            }
            
            if (_gravityTriggerObjectController != null)
            {
                Object.Destroy(_gravityTriggerObjectController);
            }
            
            base.Deactivate();
        }
    }

    internal class GravityTriggerObjectController : MonoBehaviour
    {
        private GravityTriggerObject _gravityTriggerObject;

        public void Init(GravityTriggerObject gravityTriggerObject)
        {
            _gravityTriggerObject = gravityTriggerObject;
        }

        public void Interact(GameObject playerActorGameObject)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(new PlayerEnableInputCommand(0));
            StartCoroutine(InteractInternal(playerActorGameObject));
        }

        private IEnumerator InteractInternal(GameObject playerActorGameObject)
        {
            GameObject gravityTriggerGo = _gravityTriggerObject.GetGameObject();
            var platformController = gravityTriggerGo.GetComponent<StandingPlatformController>();
            Vector3 platformPosition = platformController.transform.position;
            var actorStandingPosition = new Vector3(
                platformPosition.x,
                platformController.GetPlatformHeight(),
                platformPosition.z);
            
            var movementController = playerActorGameObject.GetComponent<ActorMovementController>();
            yield return movementController.MoveDirectlyTo(actorStandingPosition, 0);
            
            CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxCommand("we026", 1));

            var cvdModelRenderer = _gravityTriggerObject.GetCvdModelRenderer();
            yield return cvdModelRenderer.PlayOneTimeAnimation(true);
            
            CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxCommand("wg005", 1));

            Vector3 finalPosition = gravityTriggerGo.transform.position;
            finalPosition.y -= GravityTriggerObject.DescendingHeight;
            yield return AnimationHelper.MoveTransform(gravityTriggerGo.transform,
                finalPosition,
                GravityTriggerObject.DescendingAnimationDuration,
                AnimationCurveType.Sine);

            _gravityTriggerObject.InteractWithLinkedObjectIfAny();

            CommandDispatcher<ICommand>.Instance.Dispatch(new PlayerEnableInputCommand(1));
        }
    }
}