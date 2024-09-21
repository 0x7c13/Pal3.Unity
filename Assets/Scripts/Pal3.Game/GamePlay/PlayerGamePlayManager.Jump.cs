// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.GamePlay
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Nav;
    using Engine.Animation;
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
    using Engine.Coroutine;
    using Engine.Extensions;
    using Engine.Renderer;
    using Scene;
    using State;

    using Vector2 = UnityEngine.Vector2;
    using Vector2Int = UnityEngine.Vector2Int;
    using Vector3 = UnityEngine.Vector3;

    public partial class PlayerGamePlayManager
    {
        private const float MIN_JUMP_DISTANCE = 1.2f;
        private const float MAX_JUMP_DISTANCE = 8f;
        private const float MAX_JUMP_Y_DIFFERENTIAL = 3.5f;
        private const float JUMP_HEIGHT = 6f;

        private int _jumpableAreaEnterCount;

        private IGameEntity _jumpIndicatorGameEntity;
        private AnimatedBillboardRenderer _jumpIndicatorRenderer;

        private void JumpToTapPosition()
        {
            if (!_lastInputTapPosition.HasValue) return;

            if (!_physicsManager.TryCameraRaycastFromScreenPoint(_lastInputTapPosition.Value,
                    out (Vector3 hitPoint, IGameEntity colliderGameEntity) hitResult)) return;

            if (hitResult.colliderGameEntity.GetComponent<NavMesh>() != null)
            {
                Pal3.Instance.StartCoroutine(JumpAsync(hitResult.hitPoint));
            }
        }

        public void PlayerActorEnteredJumpableArea()
        {
            if (_jumpIndicatorGameEntity == null)
            {
                ISprite[] sprites = _resourceProvider.GetJumpIndicatorSprites();

                _jumpIndicatorGameEntity = GameEntityFactory.Create($"JumpIndicator",
                    _playerActorGameEntity, worldPositionStays: false);
                _jumpIndicatorGameEntity.Transform.LocalScale = new Vector3(3f, 3f, 3f);
                _jumpIndicatorGameEntity.Transform.LocalPosition = new Vector3(0f,
                    _playerActorActionController.GetActorHeight() + 1f, 0f);

                _jumpIndicatorRenderer = _jumpIndicatorGameEntity.AddComponent<AnimatedBillboardRenderer>();
                _jumpIndicatorRenderer.Init(sprites, 4);
            }

            if (_jumpableAreaEnterCount == 0)
            {
                _jumpIndicatorRenderer.StartAnimation(-1);
            }

            _jumpableAreaEnterCount++;
        }

        public void PlayerActorExitedJumpableArea()
        {
            _jumpableAreaEnterCount--;

            if (_jumpableAreaEnterCount == 0 && _jumpIndicatorRenderer != null)
            {
                _jumpIndicatorRenderer.StopAnimation();
            }
        }

        private bool IsPlayerActorInsideJumpableArea()
        {
            return _jumpableAreaEnterCount > 0;
        }

        private void ResetAndDisposeJumpIndicators()
        {
            _jumpableAreaEnterCount = 0;

            if (_jumpIndicatorRenderer != null)
            {
                _jumpIndicatorRenderer.StopAnimation();
                _jumpIndicatorRenderer.Destroy();
                _jumpIndicatorRenderer = null;
            }

            if (_jumpIndicatorGameEntity != null)
            {
                _jumpIndicatorGameEntity.Destroy();
                _jumpIndicatorGameEntity = null;
            }
        }

        private IEnumerator JumpAsync(Vector3? jumpTargetPosition = null)
        {
            _gameStateManager.TryGoToState(GameState.Cutscene);
            _playerActorMovementController.CancelMovement();

            Vector3 currentPosition = _playerActorMovementController.GetWorldPosition();

            GameScene currentScene = _sceneManager.GetCurrentScene();
            Tilemap tilemap = currentScene.GetTilemap();

            bool IsPositionCanJumpTo(Vector3 position, int layerIndex, out float yPosition, out int distanceToObstacle)
            {
                Vector2Int tilePosition = tilemap.GetTilePosition(position, layerIndex);

                if (tilemap.TryGetTile(position, layerIndex, out NavTile tile) &&
                    tile.IsWalkable())
                {
                    distanceToObstacle = tile.DistanceToNearestObstacle;
                    yPosition = tile.GameBoxYPosition.ToUnityYPosition();

                    if (MathF.Abs(yPosition - currentPosition.y) > MAX_JUMP_Y_DIFFERENTIAL) return false;

                    if (currentScene.IsPositionInsideJumpableArea(layerIndex, tilePosition))
                    {
                        return true;
                    }

                    #if PAL3
                    // Special case for PAL3 M15-B1
                    if (_sceneManager.GetCurrentScene().GetSceneInfo().Is("m15", "b1"))
                    {
                        return true;
                    }
                    #endif
                }

                yPosition = 0f;
                distanceToObstacle = 0;
                return false;
            }

            Vector3 jumpDirection = _playerActorGameEntity.Transform.Forward;

            if (jumpTargetPosition != null)
            {
                jumpDirection = jumpTargetPosition.Value - _playerActorGameEntity.Transform.Position;
                jumpDirection.y = 0f;
                jumpDirection.Normalize();
                _playerActorGameEntity.Transform.Forward = jumpDirection;
            }

            List<(Vector3 position, int layerIndex, int distanceToObstacle)> validJumpTargetPositions = new();

            for (float i = MIN_JUMP_DISTANCE; i <= MAX_JUMP_DISTANCE; i += 0.1f)
            {
                Vector3 targetPosition = currentPosition + jumpDirection * i;

                for (int j = 0; j < tilemap.GetLayerCount(); j++)
                {
                    if (IsPositionCanJumpTo(targetPosition, j,
                            out float yPosition, out int distanceToObstacle))
                    {
                        Vector3 position = targetPosition;
                        position.y = yPosition;
                        validJumpTargetPositions.Add((position, j, distanceToObstacle));
                    }
                }
            }

            int currentLayer = _playerActorMovementController.GetCurrentLayerIndex();
            int jumpTargetLayer = currentLayer;
            if (validJumpTargetPositions.Count > 0)
            {
                // If there are valid jump target positions in different layers,
                // only pick positions in the other layer
                if (validJumpTargetPositions.Any(_ => _.layerIndex != currentLayer))
                {
                    validJumpTargetPositions = validJumpTargetPositions.Where(_ => _.layerIndex != currentLayer).ToList();
                }

                // Pick a position that is farthest from obstacles
                int maxDistanceToObstacle = validJumpTargetPositions.Max(_ => _.distanceToObstacle);
                (jumpTargetPosition, jumpTargetLayer, _) = validJumpTargetPositions
                    .First(_ => _.distanceToObstacle == maxDistanceToObstacle);
            }
            else
            {
                jumpTargetPosition = currentPosition;
            }

            _playerActorActionController.PerformAction(ActorConstants.ActionToNameMap[ActorActionType.Jump],
                 overwrite: true, loopCount: 1);
            yield return CoroutineYieldInstruction.WaitForSeconds(0.7f);

            float xzOffset = Vector2.Distance(
                new Vector2(jumpTargetPosition.Value.x, jumpTargetPosition.Value.z),
                new Vector2(currentPosition.x, currentPosition.z));
            float startingYPosition = currentPosition.y;
            float yOffset = jumpTargetPosition.Value.y - currentPosition.y;

            yield return CoreAnimation.EnumerateValueAsync(0f, 1f, 1.1f, AnimationCurveType.Sine,
                value =>
                {
                    Vector3 calculatedPosition = currentPosition + jumpDirection * (xzOffset * value);
                    calculatedPosition.y = startingYPosition + (0.5f - MathF.Abs(value - 0.5f)) * JUMP_HEIGHT + yOffset * value;
                    _playerActorGameEntity.Transform.Position = calculatedPosition;
                });
            yield return CoroutineYieldInstruction.WaitForSeconds(0.7f);

            _playerActorMovementController.SetNavLayer(jumpTargetLayer);

            PlayerActorTilePositionChanged(jumpTargetLayer,
                tilemap.GetTilePosition(jumpTargetPosition.Value, jumpTargetLayer), false);

            _gameStateManager.TryGoToState(GameState.Gameplay);
        }
    }
}