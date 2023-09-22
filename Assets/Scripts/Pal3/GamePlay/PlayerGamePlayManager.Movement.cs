// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.GamePlay
{
    using System.Collections.Generic;
    using System.Linq;
    using Actor.Controllers;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Nav;
    using Scene;
    using Scene.SceneObjects.Common;
    using UnityEngine;

    public partial class PlayerGamePlayManager
    {
        private const float PLAYER_ACTOR_MOVEMENT_SFX_WALK_VOLUME = 0.6f;
        private const float PLAYER_ACTOR_MOVEMENT_SFX_RUN_VOLUME = 1.0f;

        private void ReadInputAndMovePlayerIfNeeded()
        {
            var movement = _inputActions.Gameplay.Movement.ReadValue<Vector2>();
            if (movement.magnitude <= 0.01f) return;

            MovementMode movementMode = movement.magnitude < 0.7f ? MovementMode.Walk : MovementMode.Run;
            ActorActionType movementAction = movementMode == MovementMode.Walk ? ActorActionType.Walk : ActorActionType.Run;
            _playerActorMovementController.CancelMovement();
            Transform cameraTransform = _camera.transform;
            Vector2 normalizedDirection = movement.normalized;
            Vector3 inputDirection = cameraTransform.forward * normalizedDirection.y +
                                     cameraTransform.right * normalizedDirection.x;
            MovementResult result = PlayerActorMoveTowards(inputDirection, movementMode);
            _playerActorActionController.PerformAction(result == MovementResult.Blocked
                ? ActorActionType.Stand
                : movementAction);
        }

        private void PortalToTapPosition()
        {
            if (!_lastInputTapPosition.HasValue) return;

            Ray ray = _camera.ScreenPointToRay(_lastInputTapPosition.Value);

            if (!Physics.Raycast(ray, out RaycastHit hit)) return;

            int layerIndex;
            bool isPositionOnStandingPlatform;

            if (hit.collider.gameObject.GetComponent<StandingPlatformController>() is { } standingPlatformController)
            {
                layerIndex = standingPlatformController.LayerIndex;
                isPositionOnStandingPlatform = true;
            }
            else
            {
                var meshColliders = _sceneManager.GetCurrentScene()
                    .GetMeshColliders();

                if (meshColliders.Any(_ => _.Value == hit.collider))
                {
                    layerIndex = meshColliders.First(_ => _.Value == hit.collider).Key;
                    isPositionOnStandingPlatform = false;
                }
                else
                {
                    // Raycast hit a collider that is not a mesh collider or a standing platform
                    return;
                }
            }

            _playerActorMovementController.PortalToPosition(hit.point, layerIndex, isPositionOnStandingPlatform);
        }

        // Raycast caches to avoid GC
        private readonly RaycastHit[] _raycastHits = new RaycastHit[4];
        private readonly Dictionary<int, (Vector3 point, bool isPlatform)> _tapPoints = new ();
        private void MoveToTapPosition(bool isDoubleTap)
        {
            if (!_lastInputTapPosition.HasValue) return;

            Scene currentScene = _sceneManager.GetCurrentScene();
            var meshColliders = currentScene.GetMeshColliders();

            Ray ray = _camera.ScreenPointToRay(_lastInputTapPosition.Value);

            var hitCount = Physics.RaycastNonAlloc(ray, _raycastHits, 500f);
            if (hitCount == 0) return;

            Tilemap tilemap = currentScene.GetTilemap();

            _tapPoints.Clear();
            for (var i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _raycastHits[i];

                if (hit.collider.gameObject.GetComponent<StandingPlatformController>() is { }
                        standingPlatformController)
                {
                    _tapPoints[standingPlatformController.LayerIndex] = (hit.point, true);
                    continue;
                }

                var layerIndex = meshColliders.FirstOrDefault(_ => _.Value == hit.collider).Key;

                if (!tilemap.TryGetTile(hit.point, layerIndex, out NavTile _))
                {
                    continue;
                }

                Vector3 cameraPosition = _camera.transform.position;
                var distanceToCamera = Vector3.Distance(cameraPosition, hit.point);

                if (_tapPoints.ContainsKey(layerIndex))
                {
                    var existingDistance = Vector3.Distance(cameraPosition, _tapPoints[layerIndex].point);
                    if (distanceToCamera < existingDistance)
                    {
                        _tapPoints[layerIndex] = (hit.point, false);
                    }
                }
                else
                {
                    _tapPoints[layerIndex] = (hit.point, false);
                }
            }

            if (_tapPoints.Count > 0)
            {
                _playerActorMovementController.MoveToTapPoint(_tapPoints, isDoubleTap);
            }
        }

        /// <summary>
        /// Adjust some degrees to the inputDirection to prevent player actor
        /// from hitting into the wall and gets blocked.
        /// This process is purely for improving the gameplay experience.
        /// </summary>
        /// <param name="inputDirection">User input direction in game space</param>
        /// <param name="movementMode">Player actor movement mode</param>
        /// <returns>MovementResult</returns>
        private MovementResult PlayerActorMoveTowards(Vector3 inputDirection, MovementMode movementMode)
        {
            Vector3 playerActorPosition = _playerActorGameObject.transform.position;
            MovementResult result = _playerActorMovementController.MoveTowards(
                playerActorPosition + inputDirection, movementMode);

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
                        playerActorPosition + newDirection, movementMode);
                    if (result != MovementResult.Blocked) return result;
                }
                // - degrees
                {
                    Vector3 newDirection = Quaternion.Euler(0f, -degrees, 0f) * inputDirection;
                    result = _playerActorMovementController.MoveTowards(
                        playerActorPosition + newDirection, movementMode);
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
            if ((_lastKnownPosition.HasValue && _lastKnownLayerIndex.HasValue) &&
                tileMap.TryGetTile(_lastKnownPosition.Value, _lastKnownLayerIndex.Value, out NavTile tile))
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
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new StopSfxPlayingAtGameObjectRequest(_playerActorGameObject,
                        AudioConstants.PlayerActorMovementSfxAudioSourceName,
                        disposeSource: false));
            }
            else
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new AttachSfxToGameObjectRequest(_playerActorGameObject,
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