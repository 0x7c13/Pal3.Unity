// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using Actor;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Common;
    using Core.Animation;
    using Core.DataReader.Scn;
    using Core.Services;
    using Data;
    using MetaData;
    using Player;
    using State;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [ScnSceneObject(ScnSceneObjectType.ElevatorPedal)]
    public class ElevatorPedalObject : SceneObject
    {
        public const float ElevatorSpeed = 3f;
        
        private StandingPlatformController _platformController;
        private ElevatorPedalObjectController _objectController;
        
        private readonly PlayerManager _playerManager;
        private readonly GameStateManager _gameStateManager;
        private readonly Tilemap _tilemap;

        public ElevatorPedalObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _playerManager = ServiceLocator.Instance.Get<PlayerManager>();
            _gameStateManager = ServiceLocator.Instance.Get<GameStateManager>();
            _tilemap = ServiceLocator.Instance.Get<SceneManager>().GetCurrentScene().GetTilemap();
        }
        
        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            Bounds bounds = GetPolyModelRenderer().GetMeshBounds();

            _platformController = sceneGameObject.AddComponent<StandingPlatformController>();
            _platformController.SetBounds(bounds, ObjectInfo.LayerIndex);
            _platformController.OnTriggerEntered += OnPlatformTriggerEntered;
            
            _objectController = sceneGameObject.AddComponent<ElevatorPedalObjectController>();
            _objectController.Init(this);

            // Set to init position based on layer index
            if (ObjectInfo.LayerIndex == 0)
            {
                var tilePosition = new Vector2Int(ObjectInfo.Parameters[0], ObjectInfo.Parameters[1]);
                Vector3 position = sceneGameObject.transform.position;
                position.y = _tilemap.GetWorldPosition(tilePosition, 0).y + bounds.size.y / 2f;
                sceneGameObject.transform.position = position;
            }
            else
            {
                var tilePosition = new Vector2Int(ObjectInfo.Parameters[2], ObjectInfo.Parameters[3]);
                Vector3 position = sceneGameObject.transform.position;
                position.y = _tilemap.GetWorldPosition(tilePosition, 1).y + bounds.size.y / 2f;
                sceneGameObject.transform.position = position;
            }
            
            return sceneGameObject;
        }

        private void OnPlatformTriggerEntered(object sender, Collider collider)
        {
            // Prevent duplicate triggers
            if (_gameStateManager.GetCurrentState() != GameState.Gameplay) return;
            
            // Check if the player actor is on the platform
            if (collider.gameObject.GetComponent<ActorController>() is {} actorController &&
                actorController.GetActor().Info.Id == (byte)_playerManager.GetPlayerActor())
            {
                if (!IsInteractableBasedOnTimesCount()) return;

                ToggleSwitchState();
                
                _objectController.Interact(collider.gameObject);
            }
        }

        public override void Deactivate()
        {
            if (_platformController != null)
            {
                _platformController.OnTriggerEntered -= OnPlatformTriggerEntered;
                Object.Destroy(_platformController);
            }
            
            if (_objectController != null)
            {
                Object.Destroy(_objectController);
            }
            
            base.Deactivate();
        }
    }
    
    internal class ElevatorPedalObjectController : MonoBehaviour
    {
        private ElevatorPedalObject _object;

        public void Init(ElevatorPedalObject elevatorPedalObject)
        {
            _object = elevatorPedalObject;
        }
        
        public void Interact(GameObject playerActorGameObject)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new GameStateChangeRequest(GameState.Cutscene));
            StartCoroutine(InteractInternal(playerActorGameObject));
        }
        
        private IEnumerator InteractInternal(GameObject playerActorGameObject)
        {
            var tilemap = ServiceLocator.Instance.Get<SceneManager>().GetCurrentScene().GetTilemap();
            
            byte fromLayer = _object.ObjectInfo.LayerIndex;
            byte toLayer = (byte) ((fromLayer + 1) % 2);

            var tilePositions = new Vector2Int[2];
            var positions = new Vector3[2];
            
            tilePositions[0] = new Vector2Int(_object.ObjectInfo.Parameters[0], _object.ObjectInfo.Parameters[1]);
            tilePositions[1] = new Vector2Int(_object.ObjectInfo.Parameters[2], _object.ObjectInfo.Parameters[3]);
            
            positions[0] = tilemap.GetWorldPosition(tilePositions[0], 0);
            positions[1] = tilemap.GetWorldPosition(tilePositions[1], 1);
            
            var actorMovementController = playerActorGameObject.GetComponent<ActorMovementController>();

            var platformController = _object.GetGameObject().GetComponent<StandingPlatformController>();
            var platformCenterPosition = platformController.GetCollider().bounds.center;
            var platformHeight = platformController.GetCollider().bounds.size.y / 2f;
            
            var actorStandingPosition = new Vector3(platformCenterPosition.x, positions[fromLayer].y, platformCenterPosition.z);
            var platformFinalPosition = new Vector3(platformCenterPosition.x, positions[toLayer].y + platformHeight, platformCenterPosition.z);
            
            yield return actorMovementController.MoveDirectlyTo(actorStandingPosition, 0);
            
            var duration = Vector3.Distance(positions[fromLayer], positions[toLayer]) / ElevatorPedalObject.ElevatorSpeed;
            yield return AnimationHelper.MoveTransform(transform, platformFinalPosition, duration, AnimationCurveType.Sine);
            
            _object.ChangeAndSaveNavLayerIndex(toLayer);
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorSetNavLayerCommand(ActorConstants.PlayerActorVirtualID, toLayer));
            
            yield return actorMovementController.MoveDirectlyTo(positions[toLayer], 0);
            
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new GameStateChangeRequest(GameState.Gameplay));
        }
    }
}