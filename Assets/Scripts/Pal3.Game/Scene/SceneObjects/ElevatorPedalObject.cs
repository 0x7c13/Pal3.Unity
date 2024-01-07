// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
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
    using Data;
    using Engine.Animation;
    using Engine.Core.Abstraction;
    using Engine.Extensions;
    using Engine.Services;
    using State;

    using Bounds = UnityEngine.Bounds;
    using Color = Core.Primitives.Color;
    using Vector2Int = UnityEngine.Vector2Int;
    using Vector3 = UnityEngine.Vector3;

    [ScnSceneObject(SceneObjectType.ElevatorPedal)]
    public sealed class ElevatorPedalObject : SceneObject
    {
        private const float ELEVATOR_SPPED = 3f;

        private StandingPlatformController _platformController;

        private readonly GameStateManager _gameStateManager;

        public ElevatorPedalObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _gameStateManager = ServiceLocator.Instance.Get<GameStateManager>();
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();

            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            Bounds bounds = GetMeshBounds();

            _platformController = sceneObjectGameEntity.AddComponent<StandingPlatformController>();
            _platformController.Init(bounds, ObjectInfo.LayerIndex);
            _platformController.OnPlayerActorEntered += OnPlayerActorEntered;

            Tilemap tilemap = ServiceLocator.Instance.Get<SceneManager>().GetCurrentScene().GetTilemap();
            // Set to init position based on layer index
            if (ObjectInfo.LayerIndex == 0)
            {
                var tilePosition = new Vector2Int(ObjectInfo.Parameters[0], ObjectInfo.Parameters[1]);
                Vector3 position = sceneObjectGameEntity.Transform.Position;
                position.y = tilemap.GetWorldPosition(tilePosition, 0).y + bounds.size.y / 2f;
                sceneObjectGameEntity.Transform.Position = position;
            }
            else
            {
                var tilePosition = new Vector2Int(ObjectInfo.Parameters[2], ObjectInfo.Parameters[3]);
                Vector3 position = sceneObjectGameEntity.Transform.Position;
                position.y = tilemap.GetWorldPosition(tilePosition, 1).y + bounds.size.y / 2f;
                sceneObjectGameEntity.Transform.Position = position;
            }

            return sceneObjectGameEntity;
        }

        private void OnPlayerActorEntered(object sender, IGameEntity playerActorGameEntity)
        {
            // Prevent duplicate triggers
            if (_gameStateManager.GetCurrentState() != GameState.Gameplay) return;

            if (!IsInteractableBasedOnTimesCount()) return;

            FlipAndSaveSwitchState();

            RequestForInteraction();
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            Tilemap tilemap = ctx.CurrentScene.GetTilemap();

            byte fromLayer = ObjectInfo.LayerIndex;
            byte toLayer = (byte) ((fromLayer + 1) % 2);

            var tilePositions = new Vector2Int[2];
            var positions = new Vector3[2];

            tilePositions[0] = new Vector2Int(ObjectInfo.Parameters[0], ObjectInfo.Parameters[1]);
            tilePositions[1] = new Vector2Int(ObjectInfo.Parameters[2], ObjectInfo.Parameters[3]);

            positions[0] = tilemap.GetWorldPosition(tilePositions[0], 0);
            positions[1] = tilemap.GetWorldPosition(tilePositions[1], 1);

            var actorMovementController = ctx.PlayerActorGameEntity.GetComponent<ActorMovementController>();

            IGameEntity elevatorEntity = GetGameEntity();
            Vector3 platformCenterPosition = _platformController.GetCollider().bounds.center;
            var platformHeight = _platformController.GetCollider().bounds.size.y / 2f;

            var actorStandingPosition = new Vector3(platformCenterPosition.x, positions[fromLayer].y, platformCenterPosition.z);
            var platformFinalPosition = new Vector3(platformCenterPosition.x, positions[toLayer].y + platformHeight, platformCenterPosition.z);

            yield return actorMovementController.MoveDirectlyToAsync(actorStandingPosition, 0, true);

            var duration = Vector3.Distance(positions[fromLayer], positions[toLayer]) / ELEVATOR_SPPED;
            yield return elevatorEntity.Transform.MoveAsync(platformFinalPosition,
                duration, AnimationCurveType.Sine);

            ChangeAndSaveNavLayerIndex(toLayer);
            actorMovementController.SetNavLayer(toLayer);

            yield return actorMovementController.MoveDirectlyToAsync(positions[toLayer], 0, true);
        }

        public override void Deactivate()
        {
            if (_platformController != null)
            {
                _platformController.OnPlayerActorEntered -= OnPlayerActorEntered;
                _platformController.Destroy();
                _platformController = null;
            }

            base.Deactivate();
        }
    }
}

#endif