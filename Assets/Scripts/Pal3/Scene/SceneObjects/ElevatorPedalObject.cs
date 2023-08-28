// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Actor.Controllers;
    using Common;
    using Core.Animation;
    using Core.Contracts;
    using Core.DataReader.Scn;
    using Core.Extensions;
    using Core.Services;
    using Data;
    using State;
    using UnityEngine;

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

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameObject();

            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            Bounds bounds = GetMeshBounds();

            _platformController = sceneGameObject.AddComponent<StandingPlatformController>();
            _platformController.Init(bounds, ObjectInfo.LayerIndex);
            _platformController.OnPlayerActorEntered += OnPlayerActorEntered;

            Tilemap tilemap = ServiceLocator.Instance.Get<SceneManager>().GetCurrentScene().GetTilemap();
            // Set to init position based on layer index
            if (ObjectInfo.LayerIndex == 0)
            {
                var tilePosition = new Vector2Int(ObjectInfo.Parameters[0], ObjectInfo.Parameters[1]);
                Vector3 position = sceneGameObject.transform.position;
                position.y = tilemap.GetWorldPosition(tilePosition, 0).y + bounds.size.y / 2f;
                sceneGameObject.transform.position = position;
            }
            else
            {
                var tilePosition = new Vector2Int(ObjectInfo.Parameters[2], ObjectInfo.Parameters[3]);
                Vector3 position = sceneGameObject.transform.position;
                position.y = tilemap.GetWorldPosition(tilePosition, 1).y + bounds.size.y / 2f;
                sceneGameObject.transform.position = position;
            }

            return sceneGameObject;
        }

        private void OnPlayerActorEntered(object sender, GameObject playerGameObject)
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

            var actorMovementController = ctx.PlayerActorGameObject.GetComponent<ActorMovementController>();

            GameObject elevatorGameObject = GetGameObject();
            Vector3 platformCenterPosition = _platformController.GetCollider().bounds.center;
            var platformHeight = _platformController.GetCollider().bounds.size.y / 2f;

            var actorStandingPosition = new Vector3(platformCenterPosition.x, positions[fromLayer].y, platformCenterPosition.z);
            var platformFinalPosition = new Vector3(platformCenterPosition.x, positions[toLayer].y + platformHeight, platformCenterPosition.z);

            yield return actorMovementController.MoveDirectlyToAsync(actorStandingPosition, 0, true);

            var duration = Vector3.Distance(positions[fromLayer], positions[toLayer]) / ELEVATOR_SPPED;
            yield return elevatorGameObject.transform.MoveAsync(platformFinalPosition,
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