// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Actor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using MetaData;
    using Scene;
    using Script.Waiter;
    using UnityEngine;
    using Random = UnityEngine.Random;

    public enum MovementResult
    {
        InProgress,
        Blocked,
        Completed,
    }

    public class ActorMovementController : MonoBehaviour,
        ICommandExecutor<ActorSetTilePositionCommand>,
        ICommandExecutor<ActorPathToCommand>,
        ICommandExecutor<ActorMoveBackwardsCommand>,
        ICommandExecutor<ActorMoveToCommand>,
        ICommandExecutor<ActorStopActionAndStandCommand>,
        ICommandExecutor<ActorMoveOutOfScreenCommand>,
        ICommandExecutor<ActorActivateCommand>,
        ICommandExecutor<ActorSetNavLayerCommand>,
        ICommandExecutor<PlayerActorPositionUpdatedNotification>
    {
        private const float MAX_CROSS_LAYER_Y_DIFFERENTIAL = 2f;

        private Actor _actor;
        private Tilemap _tilemap;
        private ActorActionController _actionController;
        private int _currentLayerIndex = 0;

        private Path _currentPath;
        private WaitUntilCanceled _movementWaiter;

        public void Init(Actor actor, Tilemap tilemap, ActorActionController actionController)
        {
            _actor = actor;
            _tilemap = tilemap;
            _actionController = actionController;
            _currentLayerIndex = actor.Info.OnLayer;

            var initPosition = GameBoxInterpreter.ToUnityPosition(new Vector3(actor.Info.PositionX,
                actor.Info.PositionY, actor.Info.PositionZ));

            var tilePosition = _tilemap.GetTilePosition(initPosition, _currentLayerIndex);
            if (_tilemap.IsTilePositionInsideTileMap(tilePosition, _currentLayerIndex))
            {
                var yPosition = _tilemap.GetTile(tilePosition,
                    _currentLayerIndex).Y / GameBoxInterpreter.GameBoxUnitToUnityUnit;

                transform.position = (actor.Info.PositionY == 0 || actor.Info.PositionY < yPosition) ?
                    new Vector3(initPosition.x, yPosition, initPosition.z) :
                    initPosition;
            }
            else
            {
                transform.position = initPosition;
            }
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            _currentPath?.Clear();
            _movementWaiter?.CancelWait();
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void SetNavLayer(int layerIndex)
        {
            _currentLayerIndex = layerIndex;
        }

        public bool MovementInProgress()
        {
            return _currentPath != null && !_currentPath.IsEndOfPath();
        }

        public void CancelCurrentMovement()
        {
            _currentPath?.Clear();
        }

        private void Update()
        {
            var path = _currentPath;
            if (path == null || path.IsEndOfPath()) return;

            var ignoreBlock = path.EndOfPathAction == EndOfPathActionType.Reverse;
            var result = MoveTowards(path.GetCurrentWayPoint(), path.MovementMode, ignoreBlock);

            if (result == MovementResult.Blocked)
            {
                ReachingToEndOfPath(path);
            }
            else if (result == MovementResult.Completed)
            {
                if (!path.MoveToNextWayPoint())
                {
                    ReachingToEndOfPath(path);
                }
            }
        }

        public int GetCurrentLayerIndex()
        {
            return _currentLayerIndex;
        }

        public Vector3 GetWorldPosition()
        {
            return transform.position;
        }

        public Vector2Int GetTilePosition()
        {
            return _tilemap.GetTilePosition(transform.position, _currentLayerIndex);
        }

        public void PortalToPosition(Vector3 position, int layerIndex)
        {
            if (IsNotObstacleAtLayer(position, layerIndex, out var yPosition))
            {
                _currentPath?.Clear();
                _actionController.PerformAction(_actor.GetIdleAction());
                SetNavLayer(layerIndex);
                transform.position = new Vector3(position.x, yPosition, position.z);

                Debug.Log($"Portal to tile position: " +
                          $"{_tilemap.GetTilePosition(position, layerIndex)}");
            }
        }

        public void MoveToTapPoint(Dictionary<int, Vector3> tapPoints, bool isDoubleTap)
        {
            var targetPosition = Vector3.zero;
            var targetPositionFound = false;
            if (tapPoints.ContainsKey(_currentLayerIndex))
            {
                if (IsNotObstacleAtLayer(tapPoints[_currentLayerIndex], _currentLayerIndex, out _))
                {
                    targetPosition = tapPoints[_currentLayerIndex];
                    targetPositionFound = true;
                }
            }

            var nextLayer = (_currentLayerIndex + 1) % 2;
            if (!targetPositionFound &&
                nextLayer < _tilemap.GetLayerCount() &&
                tapPoints.ContainsKey(nextLayer))
            {
                targetPosition = tapPoints[nextLayer];
                targetPositionFound = true;
            }

            if (!targetPositionFound)
            {
                targetPosition = tapPoints.First().Value;
            }

            var moveMode = isDoubleTap ? 1 : 0;
            // Keep running when actor is already in running mode
            if (_currentPath?.MovementMode == 1) moveMode = 1;
            SetupPath(new[] { targetPosition }, moveMode, EndOfPathActionType.Idle);
        }

        private bool IsNearPortalAreaOfLayer(Vector3 position, int layerIndex)
        {
            var tilePosition = _tilemap.GetTilePosition(position, layerIndex);

            if (_tilemap.IsInsidePortalArea(tilePosition, layerIndex)) return true;

            // Check nearby 8 directions
            return Enum.GetValues(typeof(Direction)).Cast<Direction>().Any(direction =>
                _tilemap.IsInsidePortalArea(tilePosition + DirectionUtils.ToVector2Int(direction), layerIndex));
        }

        private bool IsNotObstacleAtLayer(Vector3 position, int layerIndex, out float y)
        {
            y = 0;
            var tilePosition = _tilemap.GetTilePosition(position, layerIndex);
            if (!_tilemap.IsTilePositionInsideTileMap(tilePosition, layerIndex)) return false;
            var tile = _tilemap.GetTile(tilePosition, layerIndex);
            if (tile.Distance == 0) return false;
            y = tile.Y / GameBoxInterpreter.GameBoxUnitToUnityUnit;
            return true;
        }

        public MovementResult MoveTowards(Vector3 targetPosition, int movementMode, bool ignoreBlock = false)
        {
            var currentTransform = transform;
            var currentPosition = currentTransform.position;

            // TODO: Use speed info from datascript\scene.txt file
            //var speed = _actor.Info.Speed == 0 ? 2f : _actor.Info.Speed;
            var moveSpeed = movementMode == 1 ? 11f : 5f;
            var rotationSpeed = 45f;

            if (!_actor.IsMainActor()) moveSpeed /= 2f;

            var newPosition = Vector3.MoveTowards(currentPosition, targetPosition, moveSpeed * Time.deltaTime);

            var canGotoPosition = CanGotoPosition(currentPosition, newPosition, out var newYPosition);

            if (!canGotoPosition && !ignoreBlock)
            {
                return MovementResult.Blocked;
            }

            if (!canGotoPosition || Mathf.Abs(newYPosition - newPosition.y) > 3f) newYPosition = currentPosition.y;

            var moveDirection = new Vector3(
                targetPosition.x - currentPosition.x,
                0f,
                targetPosition.z - currentPosition.z);

            // Special handling for moving backwards
            if (movementMode == 2)
            {
                currentTransform.forward = moveDirection;
            }
            else
            {
                currentTransform.forward = Vector3.RotateTowards(currentTransform.forward,
                    moveDirection, rotationSpeed * Time.deltaTime, 0.0f);
            }

            currentTransform.position = new Vector3(
                newPosition.x,
                newYPosition,
                newPosition.z);

            if (Math.Abs(currentTransform.position.x - targetPosition.x) < 0.05f &&
                Math.Abs(currentTransform.position.z - targetPosition.z) < 0.05f)
            {
                return MovementResult.Completed;
            }

            return MovementResult.InProgress;
        }

        private bool CanGotoPosition(Vector3 currentPosition, Vector3 newPosition, out float newYPosition)
        {
            newYPosition = 0f;

            // New position is not blocked at current layer
            if (IsNotObstacleAtLayer(newPosition, _currentLayerIndex, out var currentLayerY))
            {
                newYPosition = currentLayerY;
                return true;
            }

            if (_tilemap.GetLayerCount() <= 1) return false;

            var nextLayer = (_currentLayerIndex + 1) % 2;

            // New position is not blocked at next layer and y offset is within range
            if (IsNotObstacleAtLayer(newPosition, nextLayer, out var nextLayerY))
            {
                if (Mathf.Abs(currentPosition.y - nextLayerY) > MAX_CROSS_LAYER_Y_DIFFERENTIAL)
                {
                    return false;
                }

                // Switching layer
                SetNavLayer(nextLayer);
                newYPosition = nextLayerY;
                return true;
            }

            // Special handling for moving between layers (Across portal area)
            if (IsNearPortalAreaOfLayer(newPosition, _currentLayerIndex) ||
                IsNearPortalAreaOfLayer(newPosition, nextLayer))
            {
                newYPosition = currentPosition.y;
                return true;
            }

            return false;
        }

        public void SetupPath(Vector3[] wayPoints, int mode, EndOfPathActionType endOfPathAction)
        {
            _currentPath = new Path(wayPoints, mode, endOfPathAction);
            _actionController.PerformAction(_actor.GetMovementAction(mode));
        }

        private void ReachingToEndOfPath(Path path)
        {
            _movementWaiter?.CancelWait();
            switch (path.EndOfPathAction)
            {
                case EndOfPathActionType.DisposeSelf:
                    CommandDispatcher<ICommand>.Instance.Dispatch(new ActorActivateCommand(_actor.Info.Id, 0));
                    break;
                case EndOfPathActionType.Idle:
                    _actionController.PerformAction(_actor.GetIdleAction());
                    break;
                case EndOfPathActionType.Reverse:
                {
                    _actionController.PerformAction(_actor.GetIdleAction());
                    var waypoints = path.GetAllWayPoints().ToList();
                    waypoints.Reverse();
                    StartCoroutine(WaitForSomeTimeAndFollowPath(waypoints.ToArray(), path.MovementMode));
                    break;
                }
            }

            // Special handling for final rotation after moving backwards
            if (path.MovementMode == 2)
            {
                transform.forward = -transform.forward;
            }

            path.Clear();
        }

        private IEnumerator WaitForSomeTimeAndFollowPath(Vector3[] waypoints, int mode)
        {
            yield return new WaitForSeconds(Random.Range(3, 8));
            SetupPath(waypoints.ToArray(), mode, EndOfPathActionType.Reverse);
        }

        private Vector2Int[] FindPathTo(Vector3 to, int layerIndex)
        {
            var fromTile = _tilemap.GetTilePosition(transform.position, layerIndex);
            var toTile = _tilemap.GetTilePosition(to, layerIndex);
            return _tilemap.FindPathToTilePosition(fromTile, toTile, layerIndex);
        }

        public IEnumerator MoveDirectlyTo(Vector3 position, int mode)
        {
            _currentPath?.Clear();
            MovementResult result;
            _actionController.PerformAction(_actor.GetMovementAction(mode));
            do
            {
                result = MoveTowards(position, mode, ignoreBlock: true);
                yield return null;
            } while (result == MovementResult.InProgress);
            _actionController.PerformAction(_actor.GetIdleAction());
        }

        private void FindPathAndMoveToTilePosition(Vector2Int position, int mode)
        {
            var fromTile = _tilemap.GetTilePosition(transform.position, _currentLayerIndex);
            var path = _tilemap.FindPathToTilePosition(fromTile,
                new Vector2Int(position.x, position.y), _currentLayerIndex);

            if (path.Length <= 0)
            {
                Debug.LogError($"Failed to find path to tile position: {position}");
                return;
            }

            _movementWaiter?.CancelWait();
            _movementWaiter = new WaitUntilCanceled(this);
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerWaitRequest(_movementWaiter));

            var wayPoints = new Vector3[path.Length];
            for (var i = 0; i < path.Length; i++)
            {
                wayPoints[i] = _tilemap.GetWorldPosition(new Vector2Int(path[i].x, path[i].y), _currentLayerIndex);
            }

            SetupPath(wayPoints, mode, EndOfPathActionType.Idle);
        }

        private void MoveToTilePosition(Vector2Int position, int mode)
        {
            MoveTo(_tilemap.GetWorldPosition(position, _currentLayerIndex), mode);
        }

        private void MoveTo(Vector3 position, int mode)
        {
            _movementWaiter?.CancelWait();
            _movementWaiter = new WaitUntilCanceled(this);
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerWaitRequest(_movementWaiter));

            var wayPoints = new [] { position };
            SetupPath(wayPoints, mode, EndOfPathActionType.Idle);
        }

        public void Execute(ActorSetTilePositionCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            transform.position = _tilemap.GetWorldPosition(new Vector2Int(command.TileXPosition,
                command.TileZPosition), _currentLayerIndex);
        }

        public void Execute(ActorPathToCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            FindPathAndMoveToTilePosition(new Vector2Int(command.TileX, command.TileZ), command.Mode);
        }

        public void Execute(ActorMoveToCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            MoveToTilePosition(new Vector2Int(command.TileX, command.TileZ), command.Mode);
        }

        public void Execute(ActorMoveBackwardsCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            var moveDistance = command.Distance / GameBoxInterpreter.GameBoxUnitToUnityUnit;
            var newPosition = transform.position +  (-transform.forward * moveDistance);
            MoveTo(newPosition, 2);
        }

        public void Execute(ActorMoveOutOfScreenCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;

            _movementWaiter?.CancelWait();
            _movementWaiter = new WaitUntilCanceled(this);
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerWaitRequest(_movementWaiter));

            var wayPoints = new[]
            {
                _tilemap.GetWorldPosition(new Vector2Int(command.TileXPosition, command.TileZPosition),
                    _currentLayerIndex),
            };

            SetupPath(wayPoints, command.Mode, EndOfPathActionType.DisposeSelf);
        }

        public void Execute(ActorStopActionAndStandCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            _currentPath?.Clear();
        }

        public void Execute(ActorSetNavLayerCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            SetNavLayer(command.LayerIndex);
        }

        public void Execute(ActorActivateCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            if (command.IsActive == 0) _currentPath?.Clear();
        }

        // TODO: Remove this
        public void Execute(PlayerActorPositionUpdatedNotification command)
        {
            if (_actor.Info.Kind == ScnActorKind.CombatNpc)
            {
                if (Vector3.Distance(command.Position, transform.position) < 10f)
                {
                    var currentPosition = transform.position;
                    var direction = currentPosition - command.Position;
                    MoveTowards(currentPosition + direction.normalized, 1);
                    transform.rotation = Quaternion.LookRotation(direction, transform.up);
                }
            }
        }
    }
}