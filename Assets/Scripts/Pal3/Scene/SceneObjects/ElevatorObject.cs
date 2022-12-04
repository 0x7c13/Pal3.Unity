// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Actor;
    using Common;
    using Core.Animation;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Core.Services;
    using Data;
    using State;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.Elevator)]
    public sealed class ElevatorObject : SceneObject
    {
        private const float ELEVATOR_SPEED = 6f;

        private TilemapTriggerController _triggerController;

        private readonly GameStateManager _gameStateManager;

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

            _triggerController = sceneGameObject.AddComponent<TilemapTriggerController>();
            _triggerController.Init(ObjectInfo.TileMapTriggerRect, ObjectInfo.LayerIndex);
            _triggerController.OnPlayerActorEntered += OnPlayerActorEntered;

            return sceneGameObject;
        }

        private void OnPlayerActorEntered(object sender, Vector2Int actorTilePosition)
        {
            // To prevent looping interaction between two elevators during the transition
            if (_gameStateManager.GetCurrentState() != GameState.Gameplay) return;

            RequestForInteraction();
        }

        public override IEnumerator Interact(InteractionContext ctx)
        {
            GameBoxRect tileRect = ObjectInfo.TileMapTriggerRect;
            var fromCenterTilePosition = new Vector2Int(
                (tileRect.Left + tileRect.Right) / 2,
                (tileRect.Top + tileRect.Bottom) / 2);
            var fromNavLayer = ObjectInfo.LayerIndex;
            var toNavLayer = ObjectInfo.Parameters[0];

            Tilemap tilemap = ctx.CurrentScene.GetTilemap();

            Vector3 fromCenterPosition = tilemap.GetWorldPosition(fromCenterTilePosition, fromNavLayer);
            Vector2Int toCenterTilePosition = tilemap.GetTilePosition(fromCenterPosition, toNavLayer);
            Vector3 toCenterPosition = tilemap.GetWorldPosition(toCenterTilePosition, toNavLayer);

            var actorMovementController = ctx.PlayerActorGameObject.GetComponent<ActorMovementController>();

            // Move the player to the center of the elevator
            yield return actorMovementController.MoveDirectlyTo(fromCenterPosition, 0);

            var duration = Vector3.Distance(fromCenterPosition, toCenterPosition) / ELEVATOR_SPEED;

            // Lifting up/down
            yield return AnimationHelper.MoveTransform(ctx.PlayerActorGameObject.transform,
                toCenterPosition, duration, AnimationCurveType.Sine);

            actorMovementController.SetNavLayer(toNavLayer);

            const float zOffset = 5f; // Move player actor outside the elevator tilemap rect
            var finalPosition = new Vector3(toCenterPosition.x, toCenterPosition.y, toCenterPosition.z + zOffset);
            finalPosition.y = tilemap.TryGetTile(finalPosition, toNavLayer, out var tile)
                ? GameBoxInterpreter.ToUnityYPosition(tile.GameBoxYPosition)
                : finalPosition.y;

            yield return actorMovementController.MoveDirectlyTo(finalPosition, 0);
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