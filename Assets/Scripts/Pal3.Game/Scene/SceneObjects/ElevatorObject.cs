// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Game.Scene.SceneObjects
{
    using System.Collections;
    using Actor.Controllers;
    using Common;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Core.Primitives;
    using Data;
    using Engine.Animation;
    using Engine.Core.Abstraction;
    using Engine.Extensions;
    using Engine.Services;
    using State;

    using Color = Core.Primitives.Color;
    using Vector2Int = UnityEngine.Vector2Int;
    using Vector3 = UnityEngine.Vector3;

    [ScnSceneObject(SceneObjectType.Elevator)]
    public sealed class ElevatorObject : SceneObject
    {
        private const float ELEVATOR_SPEED = 9f;

        private TilemapTriggerController _triggerController;

        private readonly GameStateManager _gameStateManager;

        public ElevatorObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _gameStateManager = ServiceLocator.Instance.Get<GameStateManager>();
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();

            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            _triggerController = sceneObjectGameEntity.AddComponent<TilemapTriggerController>();
            _triggerController.Init(ObjectInfo.TileMapTriggerRect, ObjectInfo.LayerIndex);
            _triggerController.OnPlayerActorEntered += OnPlayerActorEntered;

            return sceneObjectGameEntity;
        }

        private void OnPlayerActorEntered(object sender, (int x, int y) tilePosition)
        {
            // To prevent looping interaction between two elevators during the transition
            if (_gameStateManager.GetCurrentState() != GameState.Gameplay) return;

            RequestForInteraction();
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
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

            var actorMovementController = ctx.PlayerActorGameEntity.GetComponent<ActorMovementController>();

            // Move the player to the center of the elevator
            yield return actorMovementController.MoveDirectlyToAsync(fromCenterPosition, 0, true);

            var duration = Vector3.Distance(fromCenterPosition, toCenterPosition) / ELEVATOR_SPEED;

            PlaySfx("wc014");

            // Lifting up/down
            yield return ctx.PlayerActorGameEntity.Transform.MoveAsync(toCenterPosition,
                duration, AnimationCurveType.Sine);

            actorMovementController.SetNavLayer(toNavLayer);

            const float zOffset = 5f; // Move player actor outside the elevator tilemap rect
            var finalPosition = new Vector3(toCenterPosition.x, toCenterPosition.y, toCenterPosition.z + zOffset);
            finalPosition.y = tilemap.TryGetTile(finalPosition, toNavLayer, out var tile)
                ? tile.GameBoxYPosition.ToUnityYPosition()
                : finalPosition.y;

            yield return actorMovementController.MoveDirectlyToAsync(finalPosition, 0, true);
        }

        public override void Deactivate()
        {
            if (_triggerController != null)
            {
                _triggerController.OnPlayerActorEntered -= OnPlayerActorEntered;
                _triggerController.Destroy();
                _triggerController = null;
            }

            base.Deactivate();
        }
    }
}

#endif