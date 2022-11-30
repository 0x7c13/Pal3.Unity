// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
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

    [ScnSceneObject(ScnSceneObjectType.Elevator)]
    public class ElevatorObject : SceneObject
    {
        public const float ElevatorSpeed = 6f;
        
        private ElevatorObjectController _objectController;
        private TilemapAutoTriggerController _triggerController;
        private GameStateManager _gameStateManager;

        public ElevatorObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _gameStateManager = ServiceLocator.Instance.Get<GameStateManager>();
        }

        public override GameObject Activate(GameResourceProvider resourceProvider,
            Color tintColor)
        {
            if (Activated) return GetGameObject();
            
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            
            _triggerController = sceneGameObject.AddComponent<TilemapAutoTriggerController>();
            _triggerController.Init(ObjectInfo.TileMapTriggerRect, ObjectInfo.LayerIndex);
            _triggerController.OnTriggerEntered += OnTriggerEntered;

            _objectController = sceneGameObject.AddComponent<ElevatorObjectController>();
            _objectController.Init(this);
            
            return sceneGameObject;
        }

        private void OnTriggerEntered(object sender, Vector2Int actorTilePosition)
        {
            // To prevent looping interaction between two elevators during the transition
            if (_gameStateManager.GetCurrentState() != GameState.Gameplay) return;
            
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
            
            if (_triggerController != null)
            {
                _triggerController.OnTriggerEntered -= OnTriggerEntered;
                Object.Destroy(_triggerController);
            }
            
            base.Deactivate();
        }
    }

    internal class ElevatorObjectController : MonoBehaviour
    {
        private ElevatorObject _object;
        
        public void Init(ElevatorObject elevatorObject)
        {
            _object = elevatorObject;
        }
        
        public void Interact()
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new GameStateChangeRequest(GameState.Cutscene));
            StartCoroutine(InteractInternal());
        }

        private IEnumerator InteractInternal()
        {
            var playerActorId = (int)ServiceLocator.Instance.Get<PlayerManager>().GetPlayerActor();
            var currentScene = ServiceLocator.Instance.Get<SceneManager>().GetCurrentScene();
            
            var tileRect = _object.ObjectInfo.TileMapTriggerRect;
            var fromCenterTilePosition = new Vector2Int(
                (tileRect.Left + tileRect.Right) / 2,
                (tileRect.Top + tileRect.Bottom) / 2);
            var fromNavLayer = _object.ObjectInfo.LayerIndex;
            var toNavLayer = _object.ObjectInfo.Parameters[0];

            Tilemap tilemap = currentScene.GetTilemap();

            Vector3 fromCenterPosition = tilemap.GetWorldPosition(fromCenterTilePosition, fromNavLayer);
            Vector2Int toCenterTilePosition = tilemap.GetTilePosition(fromCenterPosition, toNavLayer);
            Vector3 toCenterPosition = tilemap.GetWorldPosition(toCenterTilePosition, toNavLayer);
            
            GameObject playerActorGameObject = currentScene.GetActorGameObject(playerActorId);

            var actorMovementController = playerActorGameObject.GetComponent<ActorMovementController>();
            
            // Move the player to the center of the elevator
            yield return actorMovementController.MoveDirectlyTo(fromCenterPosition, 0);

            var duration = Vector3.Distance(fromCenterPosition, toCenterPosition) / ElevatorObject.ElevatorSpeed;
            
            // Lifting up/down
            yield return AnimationHelper.MoveTransform(playerActorGameObject.transform,
                toCenterPosition, duration, AnimationCurveType.Sine);
            
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorSetNavLayerCommand(ActorConstants.PlayerActorVirtualID, toNavLayer));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorSetTilePositionCommand(ActorConstants.PlayerActorVirtualID,
                    toCenterTilePosition.x, toCenterTilePosition.y));

            var zOffset = 5f; // Move player actor outside the elevator tilemap rect
            var finalPosition = new Vector3(toCenterPosition.x, toCenterPosition.y, toCenterPosition.z + zOffset);
            yield return actorMovementController.MoveDirectlyTo(finalPosition, 0);

            CommandDispatcher<ICommand>.Instance.Dispatch(
                new GameStateChangeRequest(GameState.Gameplay));
        }
    }
}