// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.GamePlay
{
    using System.Collections.Generic;
    using Actor.Controllers;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Nav;
    using Engine.Core.Abstraction;
    using Scene;
    using Scene.SceneObjects.Common;

    using Quaternion = UnityEngine.Quaternion;
    using Vector2 = UnityEngine.Vector2;
    using Vector3 = UnityEngine.Vector3;

    public partial class PlayerGamePlayManager
    {
        private const float PLAYER_ACTOR_MOVEMENT_SFX_WALK_VOLUME = 0.6f;
        private const float PLAYER_ACTOR_MOVEMENT_SFX_RUN_VOLUME = 1.0f;

        private void ReadInputAndMovePlayerIfNeeded(float deltaTime)
        {
            var movement = _inputActions.Gameplay.Movement.ReadValue<Vector2>();
            if (movement.magnitude <= 0.01f) return;

            MovementMode movementMode = movement.magnitude < 0.7f ? MovementMode.Walk : MovementMode.Run;
            ActorActionType movementAction = movementMode == MovementMode.Walk ? ActorActionType.Walk : ActorActionType.Run;
            _playerActorMovementController.CancelMovement();
            ITransform cameraTransform = _cameraManager.GetCameraTransform();
            Vector2 normalizedDirection = movement.normalized;
            Vector3 inputDirection = cameraTransform.Forward * normalizedDirection.y +
                                     cameraTransform.Right * normalizedDirection.x;
            MovementResult result = PlayerActorMoveTowards(inputDirection, movementMode, deltaTime);
            _playerActorActionController.PerformAction(result == MovementResult.Blocked
                ? ActorActionType.Stand
                : movementAction);
        }

        private void PortalToTapPosition()
        {
            if (!_lastInputTapPosition.HasValue) return;

            if (!_physicsManager.TryCameraRaycastFromScreenPoint(_lastInputTapPosition.Value,
                    out (Vector3 hitPoint, IGameEntity colliderGameEntity) hitResult)) return;

            int layerIndex;
            bool isPositionOnStandingPlatform;

            if (hitResult.colliderGameEntity.GetComponent<StandingPlatformController>() is { } standingPlatformController)
            {
                layerIndex = standingPlatformController.LayerIndex;
                isPositionOnStandingPlatform = true;
            }
            else if (hitResult.colliderGameEntity.GetComponent<NavMesh>() is { } navMesh)
            {
                layerIndex = navMesh.NavLayerIndex;
                isPositionOnStandingPlatform = false;
            }
            else // Raycast hit a collider that is neither a standing platform nor a navmesh
            {
                return; // Do nothing
            }

            _playerActorMovementController.PortalToPosition(hitResult.hitPoint, layerIndex, isPositionOnStandingPlatform);
        }

        // Raycast caches to avoid GC
        private readonly (Vector3 hitPoint, IGameEntity colliderGameEntity)[] _hitResults = new (Vector3, IGameEntity)[10];
        private readonly Dictionary<int, (Vector3 point, bool isPlatform)> _validTapPoints = new ();
        private void MoveToTapPosition(bool isDoubleTap)
        {
            if (!_lastInputTapPosition.HasValue) return;

            Scene currentScene = _sceneManager.GetCurrentScene();

            int hitCount = _physicsManager.CameraRaycastFromScreenPoint(
                _lastInputTapPosition.Value,
                _hitResults);

            if (hitCount == 0) return;

            Tilemap tilemap = currentScene.GetTilemap();

            _validTapPoints.Clear();
            for (var i = 0; i < hitCount; i++)
            {
                (Vector3 hitPoint, IGameEntity colliderGameEntity) = _hitResults[i];

                if (colliderGameEntity.GetComponent<ActorController>() != null)
                {
                    continue; // Ignore actor
                }

                if (colliderGameEntity.GetComponent<StandingPlatformController>() is { }
                        standingPlatformController)
                {
                    _validTapPoints[standingPlatformController.LayerIndex] = (hitPoint, true);
                    continue;
                }

                int layerIndex = _playerActorManager.LastKnownLayerIndex ?? 0;

                if (colliderGameEntity.GetComponent<NavMesh>() is { } navMesh)
                {
                    layerIndex = navMesh.NavLayerIndex;
                }

                if (!tilemap.TryGetTile(hitPoint, layerIndex, out NavTile _))
                {
                    continue;
                }

                Vector3 cameraPosition = _cameraManager.GetCameraTransform().Position;
                var distanceToCamera = Vector3.Distance(cameraPosition, hitPoint);

                if (_validTapPoints.ContainsKey(layerIndex))
                {
                    var existingDistance = Vector3.Distance(cameraPosition, _validTapPoints[layerIndex].point);
                    if (distanceToCamera < existingDistance)
                    {
                        _validTapPoints[layerIndex] = (hitPoint, false);
                    }
                }
                else
                {
                    _validTapPoints[layerIndex] = (hitPoint, false);
                }
            }

            if (_validTapPoints.Count > 0)
            {
                _playerActorMovementController.MoveToTapPoint(_validTapPoints, isDoubleTap);
            }
        }

        /// <summary>
        /// Adjust some degrees to the inputDirection to prevent player actor
        /// from hitting into the wall and gets blocked.
        /// This process is purely for improving the gameplay experience.
        /// </summary>
        /// <param name="inputDirection">User input direction in game space</param>
        /// <param name="movementMode">Player actor movement mode</param>
        /// <param name="deltaTime">Delta time</param>
        /// <returns>MovementResult</returns>
        private MovementResult PlayerActorMoveTowards(Vector3 inputDirection, MovementMode movementMode, float deltaTime)
        {
            Vector3 playerActorPosition = _playerActorGameEntity.Transform.Position;

            float maxDistanceDelta = _playerActor.GetMovementSpeed(movementMode) * deltaTime;
            float maxRadiansDelta = _playerActor.GetRotationSpeed() * deltaTime;

            MovementResult result = _playerActorMovementController.MoveTowards(
                playerActorPosition + inputDirection,
                movementMode,
                ignoreObstacle: false,
                maxDistanceDelta,
                maxRadiansDelta);

            if (result != MovementResult.Blocked) return result;

            if (_playerActorMovementController.IsDuringCollision()) return result;

            // Don't adjust direction if player actor is inside jumpable area
            if (IsPlayerActorInsideJumpableArea()) return result;

            // Try change direction a little bit to see if it works
            for (var degrees = 2; degrees <= 80; degrees+= 2)
            {
                // + degrees
                {
                    Vector3 newDirection = Quaternion.Euler(0f, degrees, 0f) * inputDirection;
                    result = _playerActorMovementController.MoveTowards(
                        playerActorPosition + newDirection,
                        movementMode,
                        ignoreObstacle: false,
                        maxDistanceDelta,
                        maxRadiansDelta);
                    if (result != MovementResult.Blocked) return result;
                }
                // - degrees
                {
                    Vector3 newDirection = Quaternion.Euler(0f, -degrees, 0f) * inputDirection;
                    result = _playerActorMovementController.MoveTowards(
                        playerActorPosition + newDirection,
                        movementMode,
                        ignoreObstacle: false,
                        maxDistanceDelta,
                        maxRadiansDelta);
                    if (result != MovementResult.Blocked) return result;
                }
            }

            return result;
        }

        private string GetMovementSfxName(ActorActionType movementAction)
        {
            if (movementAction is not (ActorActionType.Walk or ActorActionType.Run)) return string.Empty;

            #if PAL3
            var sfxPrefix = movementAction == ActorActionType.Walk ?
                AudioConstants.MainActorWalkSfxNamePrefix : AudioConstants.MainActorRunSfxNamePrefix;

            Tilemap tileMap = _sceneManager.GetCurrentScene().GetTilemap();
            if ((_playerActorManager.LastKnownPosition.HasValue &&
                 _playerActorManager.LastKnownLayerIndex.HasValue) &&
                tileMap.TryGetTile(_playerActorManager.LastKnownPosition.Value,
                    _playerActorManager.LastKnownLayerIndex.Value, out NavTile tile))
            {
                return tile.FloorType switch
                {
                    FloorType.Grass => sfxPrefix + 'b',
                    FloorType.Snow => sfxPrefix + 'c',
                    FloorType.Sand => sfxPrefix + 'd',
                    _ => sfxPrefix + 'a'
                };
            }
            else
            {
                return sfxPrefix + 'a';
            }
            #elif PAL3A
            if (movementAction == ActorActionType.Walk)
            {
                return AudioConstants.MainActorWalkSfxName;
            }
            else
            {
                return _playerActor.Id == (int)PlayerActorId.WangPengXu ?
                    AudioConstants.WangPengXuFlySfxName : AudioConstants.MainActorRunSfxName;
            }
            #endif
        }

        private void UpdatePlayerActorMovementSfx(string playerActorAction)
        {
            var newMovementSfxAudioFileName = string.Empty;
            ActorActionType actionType = ActorActionType.Walk;

            if (string.Equals(playerActorAction, ActorConstants.ActionToNameMap[ActorActionType.Walk]))
            {
                newMovementSfxAudioFileName = GetMovementSfxName(ActorActionType.Walk);
                actionType = ActorActionType.Walk;
            }
            else if (string.Equals(playerActorAction, ActorConstants.ActionToNameMap[ActorActionType.Run]))
            {
                newMovementSfxAudioFileName = GetMovementSfxName(ActorActionType.Run);
                actionType = ActorActionType.Run;
            }

            if (string.Equals(_currentMovementSfxAudioName, newMovementSfxAudioFileName)) return;

            _currentMovementSfxAudioName = newMovementSfxAudioFileName;

            if (string.IsNullOrEmpty(_currentMovementSfxAudioName))
            {
                Pal3.Instance.Execute(new StopSfxPlayingAtGameEntityRequest(_playerActorGameEntity,
                        AudioConstants.PlayerActorMovementSfxAudioSourceName,
                        disposeSource: false));
            }
            else
            {
                Pal3.Instance.Execute(new AttachSfxToGameEntityRequest(_playerActorGameEntity,
                        newMovementSfxAudioFileName,
                        AudioConstants.PlayerActorMovementSfxAudioSourceName,
                        loopCount: -1,
                        actionType == ActorActionType.Walk
                            ? PLAYER_ACTOR_MOVEMENT_SFX_WALK_VOLUME
                            : PLAYER_ACTOR_MOVEMENT_SFX_RUN_VOLUME,
                        startDelayInSeconds: 0f,
                        interval: 0f));
            }
        }
    }
}