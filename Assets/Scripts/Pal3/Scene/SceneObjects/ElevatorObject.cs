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
    using Core.GameBox;
    using Core.Services;
    using Data;
    using MetaData;
    using Player;
    using State;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.Elevator)]
    public class ElevatorObject : SceneObject
    {
        private const float ELEVATOR_SPEED = 6f;

        private TilemapTriggerController _triggerController;

        private readonly GameStateManager _gameStateManager;
        private readonly SceneManager _sceneManager;
        private readonly PlayerManager _playerManager;

        public ElevatorObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _gameStateManager = ServiceLocator.Instance.Get<GameStateManager>();
            _sceneManager = ServiceLocator.Instance.Get<SceneManager>();
            _playerManager = ServiceLocator.Instance.Get<PlayerManager>();
        }

        public override GameObject Activate(GameResourceProvider resourceProvider,
            Color tintColor)
        {
            if (Activated) return GetGameObject();

            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            _triggerController = sceneGameObject.AddComponent<TilemapTriggerController>();
            _triggerController.Init(ObjectInfo.TileMapTriggerRect, ObjectInfo.LayerIndex);
            _triggerController.OnPlayerActorEntered += OnPlayerActorEntered;

            return sceneGameObject;
        }

        private void OnPlayerActorEntered(object sender, Vector2Int actorTilePosition)
        {
            // To prevent looping interaction between two elevators during the transition
            if (_gameStateManager.GetCurrentState() != GameState.Gameplay) return;

            CommandDispatcher<ICommand>.Instance.Dispatch(
                new GameStateChangeRequest(GameState.Cutscene));
            Pal3.Instance.StartCoroutine(Interact(true));
        }

        public override IEnumerator Interact(bool triggerredByPlayer)
        {
            var playerActorId = (int)_playerManager.GetPlayerActor();
            var currentScene = _sceneManager.GetCurrentScene();

            var tileRect = ObjectInfo.TileMapTriggerRect;
            var fromCenterTilePosition = new Vector2Int(
                (tileRect.Left + tileRect.Right) / 2,
                (tileRect.Top + tileRect.Bottom) / 2);
            var fromNavLayer = ObjectInfo.LayerIndex;
            var toNavLayer = ObjectInfo.Parameters[0];

            Tilemap tilemap = currentScene.GetTilemap();

            Vector3 fromCenterPosition = tilemap.GetWorldPosition(fromCenterTilePosition, fromNavLayer);
            Vector2Int toCenterTilePosition = tilemap.GetTilePosition(fromCenterPosition, toNavLayer);
            Vector3 toCenterPosition = tilemap.GetWorldPosition(toCenterTilePosition, toNavLayer);

            GameObject playerActorGameObject = currentScene.GetActorGameObject(playerActorId);

            var actorMovementController = playerActorGameObject.GetComponent<ActorMovementController>();

            // Move the player to the center of the elevator
            yield return actorMovementController.MoveDirectlyTo(fromCenterPosition, 0);

            var duration = Vector3.Distance(fromCenterPosition, toCenterPosition) / ELEVATOR_SPEED;

            // Lifting up/down
            yield return AnimationHelper.MoveTransform(playerActorGameObject.transform,
                toCenterPosition, duration, AnimationCurveType.Sine);

            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorSetNavLayerCommand(ActorConstants.PlayerActorVirtualID, toNavLayer));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorSetTilePositionCommand(ActorConstants.PlayerActorVirtualID,
                    toCenterTilePosition.x, toCenterTilePosition.y));

            const float zOffset = 5f; // Move player actor outside the elevator tilemap rect
            var finalPosition = new Vector3(toCenterPosition.x, toCenterPosition.y, toCenterPosition.z + zOffset);
            finalPosition.y = tilemap.TryGetTile(finalPosition, toNavLayer, out var tile)
                ? GameBoxInterpreter.ToUnityYPosition(tile.GameBoxYPosition)
                : finalPosition.y;

            yield return actorMovementController.MoveDirectlyTo(finalPosition, 0);

            CommandDispatcher<ICommand>.Instance.Dispatch(
                new GameStateChangeRequest(GameState.Gameplay));
        }

        public override void Deactivate()
        {
            if (_triggerController != null)
            {
                _triggerController.OnPlayerActorEntered -= OnPlayerActorEntered;
                Object.Destroy(_triggerController);
            }

            base.Deactivate();
        }
    }
}