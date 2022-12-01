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
    using System.Threading;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataReader.Nav;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Core.Utils;
    using MetaData;
    using Scene;
    using Scene.SceneObjects.Common;
    using Script.Waiter;
    using UnityEngine;
    using Random = UnityEngine.Random;

    public enum MovementResult
    {
        InProgress,
        Blocked,
        Completed,
    }

    internal class ActiveColliderInfo
    {
        public Collider Collider;
        public Vector3 ActorPositionWhenCollisionEnter;
    }

    internal class ActiveStandingPlatformInfo
    {
        public StandingPlatformController Platform;
        public Vector3 PlatformLastKnownPosition;
    }

    public class ActorMovementController : MonoBehaviour,
        ICommandExecutor<ActorSetTilePositionCommand>,
        ICommandExecutor<ActorSetWorldPositionCommand>,
        ICommandExecutor<ActorPathToCommand>,
        #if PAL3A
        ICommandExecutor<ActorWalkToUsingActionCommand>,
        #endif
        ICommandExecutor<ActorMoveBackwardsCommand>,
        ICommandExecutor<ActorMoveToCommand>,
        ICommandExecutor<ActorStopActionAndStandCommand>,
        ICommandExecutor<ActorMoveOutOfScreenCommand>,
        ICommandExecutor<ActorActivateCommand>,
        ICommandExecutor<ActorSetNavLayerCommand>
    {
        private const float MAX_Y_DIFFERENTIAL = 2f;
        private const float MAX_Y_DIFFERENTIAL_CROSS_LAYER = 2f;
        private const float MAX_Y_DIFFERENTIAL_CROSS_PLATFORM = 1.5f;
        private const float DEFAULT_ROTATION_SPEED = 20f;

        private Actor _actor;
        private Tilemap _tilemap;
        private ActorActionController _actionController;
        private int _currentLayerIndex = 0;

        private readonly Path _currentPath = new ();
        private bool _isMovementOnHold;
        private WaitUntilCanceled _movementWaiter;
        private CancellationTokenSource _movementCts = new ();

        private readonly HashSet<ActiveColliderInfo> _activeColliders = new ();
        private readonly HashSet<ActiveStandingPlatformInfo> _activeStandingPlatforms = new ();

        private Func<int, int[], HashSet<Vector2Int>> _getAllActiveActorBlockingTilePositions;

        public void Init(Actor actor, Tilemap tilemap, ActorActionController actionController,
            Func<int, int[], HashSet<Vector2Int>> getAllActiveActorBlockingTilePositions)
        {
            _actor = actor;
            _tilemap = tilemap;
            _actionController = actionController;
            _currentLayerIndex = actor.Info.LayerIndex;
            _getAllActiveActorBlockingTilePositions = getAllActiveActorBlockingTilePositions;

            Vector3 initPosition = GameBoxInterpreter.ToUnityPosition(new Vector3(
                actor.Info.GameBoxXPosition,
                actor.Info.GameBoxYPosition,
                actor.Info.GameBoxZPosition));

            if (actor.Info.InitBehaviour != ScnActorBehaviour.Hold &&
                _tilemap.TryGetTile(initPosition, _currentLayerIndex, out NavTile tile))
            {
                if (tile.IsWalkable())
                {
                    transform.position = new Vector3(initPosition.x,
                        GameBoxInterpreter.ToUnityYPosition(tile.GameBoxYPosition),
                        initPosition.z);
                }
                else
                {
                    Vector2Int tilePosition = _tilemap.GetTilePosition(initPosition, _currentLayerIndex);
                    // Snap to the nearest adjacent tile if exists
                    var hasAdjacentWalkableTile =_tilemap.TryGetAdjacentWalkableTile(tilePosition,
                        _currentLayerIndex, out Vector2Int nearestTile);
                    transform.position = hasAdjacentWalkableTile ?
                        _tilemap.GetWorldPosition(nearestTile, _currentLayerIndex) :
                        initPosition;
                }
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
            _currentPath.Clear();
            _movementWaiter?.CancelWait();
            _movementCts?.Cancel();
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void SetNavLayer(int layerIndex)
        {
            _currentLayerIndex = layerIndex;
        }

        public bool IsDuringCollision()
        {
            return _activeColliders.Count > 0;
        }

        private bool IsNearOrOnTopOfPlatform()
        {
            return _activeStandingPlatforms.Count > 0;
        }

        private bool TryGetNearestActiveStandingPlatform(out ActiveStandingPlatformInfo platformInfo)
        {
            platformInfo = null;
            var distance = float.MaxValue;

            foreach (ActiveStandingPlatformInfo info in _activeStandingPlatforms)
            {
                if (info.Platform == null)
                {
                    continue; // In case the platform is destroyed
                }

                var distanceToPlatformCenter = Vector3.Distance(
                    info.Platform.GetCollider().bounds.center, transform.position);

                if (distanceToPlatformCenter < distance)
                {
                    distance = distanceToPlatformCenter;
                    platformInfo = info;
                }
            }

            return platformInfo != null;
        }

        private Vector3 GetNearestLastKnownPositionWhenCollisionEnter()
        {
            Vector3 nearestPosition = transform.position;
            float distance = float.MaxValue;

            foreach (var colliderInfo in _activeColliders)
            {
                var distanceToLastKnownPosition = Vector3.Distance(
                    colliderInfo.ActorPositionWhenCollisionEnter, transform.position);
                if (distanceToLastKnownPosition < distance)
                {
                    distance = distanceToLastKnownPosition;
                    nearestPosition = colliderInfo.ActorPositionWhenCollisionEnter;
                }
            }

            return nearestPosition;
        }

        public bool IsMovementInProgress()
        {
            return !_currentPath.IsEndOfPath();
        }

        public void CancelMovement()
        {
            _currentPath.Clear();
        }

        public void PauseMovement()
        {
            _isMovementOnHold = true;

            if (IsMovementInProgress())
            {
                _actionController.PerformAction(_actor.GetIdleAction());
            }
        }

        public void ResumeMovement()
        {
            if (_isMovementOnHold)
            {
                _isMovementOnHold = false;
                if (IsMovementInProgress())
                {
                    _actionController.PerformAction(_actor.GetMovementAction(_currentPath.MovementMode));
                }
            }
        }

        private void Update()
        {
            if (_isMovementOnHold || _currentPath.IsEndOfPath()) return;

            MovementResult result = MoveTowards(_currentPath.GetCurrentWayPoint(),
                _currentPath.MovementMode,
                _currentPath.IgnoreObstacle);

            switch (result)
            {
                case MovementResult.Blocked:
                    ReachingToEndOfPath();
                    break;
                case MovementResult.Completed:
                {
                    if (!_currentPath.MoveToNextWayPoint())
                    {
                        ReachingToEndOfPath();
                    }
                    break;
                }
            }
        }

        private void LateUpdate()
        {
            // To sync with the platform movement
            if (IsNearOrOnTopOfPlatform() && TryGetNearestActiveStandingPlatform(
                    out ActiveStandingPlatformInfo platformInfo))
            {
                Vector3 currentPlatformPosition = platformInfo.Platform.gameObject.transform.position;

                if (currentPlatformPosition != platformInfo.PlatformLastKnownPosition)
                {
                    transform.position += currentPlatformPosition - platformInfo.PlatformLastKnownPosition;
                    platformInfo.PlatformLastKnownPosition = currentPlatformPosition;
                }
            }
        }

        private void FixedUpdate()
        {
            // Sanity cleanup
            if (_activeColliders.Count > 0)
            {
                _activeColliders.RemoveWhere(_ => _.Collider == null);
            }

            // Sanity cleanup
            if (_activeStandingPlatforms.Count > 0)
            {
                _activeStandingPlatforms.RemoveWhere(_ => _.Platform == null);
            }

            // To prevent actor from bouncing into un-walkable tile position,
            // we need to reset its position during the collision.
            // Also we need to adjust Y position based on tile information
            // during the collision since we are locking Y movement for the
            // player actor's rigidbody.
            if (IsDuringCollision() && _actionController.GetRigidBody() is { isKinematic: false } _)
            {
                Vector3 currentPosition = transform.position;

                if (IsNearOrOnTopOfPlatform())
                {
                    // Do nothing.
                }
                else if (!_tilemap.TryGetTile(currentPosition, _currentLayerIndex, out NavTile tile))
                {
                    transform.position = GetNearestLastKnownPositionWhenCollisionEnter();
                }
                else
                {
                    if (!tile.IsWalkable())
                    {
                        transform.position = GetNearestLastKnownPositionWhenCollisionEnter();
                    }
                    else
                    {
                        var adjustedPosition = new Vector3(currentPosition.x,
                            GameBoxInterpreter.ToUnityYPosition(tile.GameBoxYPosition),
                            currentPosition.z);
                        transform.position = adjustedPosition;
                    }
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            Vector3 currentActorPosition = transform.position;
            Vector2Int currentTilePosition = _tilemap.GetTilePosition(currentActorPosition, _currentLayerIndex);

            Vector3 actorPosition;
            if (IsNearOrOnTopOfPlatform())
            {
                actorPosition = currentActorPosition;
            }
            else if (_tilemap.TryGetTile(currentActorPosition, _currentLayerIndex, out NavTile tile) && tile.IsWalkable())
            {
                actorPosition = currentActorPosition;
            }
            else
            {
                actorPosition =
                    _tilemap.TryGetAdjacentWalkableTile(currentTilePosition,
                        _currentLayerIndex,
                        out Vector2Int nearestWalkableTilePosition)
                        ? _tilemap.GetWorldPosition(nearestWalkableTilePosition, _currentLayerIndex)
                        : currentActorPosition; // Highly unlikely to happen, but this is the best effort
            }

            _activeColliders.Add(new ActiveColliderInfo()
            {
                Collider = collision.collider,
                ActorPositionWhenCollisionEnter = actorPosition
            });
        }

        private void OnCollisionExit(Collision collision)
        {
            _activeColliders.RemoveWhere(_ => _.Collider == collision.collider);

            if (_actionController.GetRigidBody() is { isKinematic: false } actorRigidbody)
            {
                actorRigidbody.velocity = Vector3.zero;
            }
        }

        private void OnTriggerEnter(Collider triggerCollider)
        {
            if (triggerCollider.gameObject.GetComponent<StandingPlatformController>() is { } standingPlatformController)
            {
                _activeStandingPlatforms.Add(new ActiveStandingPlatformInfo()
                    {
                        Platform = standingPlatformController,
                        PlatformLastKnownPosition = triggerCollider.gameObject.transform.position
                    });

                if (TryGetNearestActiveStandingPlatform(out ActiveStandingPlatformInfo platformInfo) &&
                    platformInfo.Platform == standingPlatformController)
                {
                    // Move the actor to the platform surface
                    Vector3 currentPosition = transform.position;
                    var targetYPosition = standingPlatformController.GetPlatformHeight();
                    if (Mathf.Abs(currentPosition.y - targetYPosition) <= MAX_Y_DIFFERENTIAL_CROSS_PLATFORM)
                    {
                        transform.position = new Vector3(currentPosition.x, targetYPosition, currentPosition.z);
                    }
                }
            }
        }

        private void OnTriggerExit(Collider triggerCollider)
        {
            if (triggerCollider.gameObject.GetComponent<StandingPlatformController>() is { } standingPlatformController)
            {
                _activeStandingPlatforms.RemoveWhere(_ => _.Platform == standingPlatformController);
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

        public void PortalToPosition(Vector3 position, int layerIndex, bool isPositionOnStandingPlatform)
        {
            if (isPositionOnStandingPlatform)
            {
                _currentPath?.Clear();
                _actionController.PerformAction(_actor.GetIdleAction());
                SetNavLayer(layerIndex);
                transform.position = position;

                Debug.LogWarning($"Portal to standing platform: {position}, " +
                                 $"layer: {layerIndex}");
            }
            else if (_tilemap.TryGetTile(position, layerIndex, out NavTile tile) && tile.IsWalkable())
            {
                _currentPath?.Clear();
                _actionController.PerformAction(_actor.GetIdleAction());
                SetNavLayer(layerIndex);
                transform.position = new Vector3(
                    position.x,
                    GameBoxInterpreter.ToUnityYPosition(tile.GameBoxYPosition),
                    position.z);

                Vector2Int tilePosition = _tilemap.GetTilePosition(position, layerIndex);
                Debug.LogWarning($"Portal to: {position}, " +
                                 $"layer: {layerIndex}, " +
                                 $"tile: {tilePosition} DistanceToNearestObstacle: {tile.DistanceToNearestObstacle}, FloorKind: {tile.FloorKind}");
            }
        }

        public void MoveToTapPoint(Dictionary<int, (Vector3 point, bool isPlatform)> tapPoints, bool isDoubleTap)
        {
            Vector3 targetPosition = Vector3.zero;
            var targetPositionFound = false;
            if (tapPoints.ContainsKey(_currentLayerIndex))
            {
                if (tapPoints[_currentLayerIndex].isPlatform ||
                    (_tilemap.TryGetTile(tapPoints[_currentLayerIndex].point, _currentLayerIndex, out NavTile tile) &&
                    tile.IsWalkable()))
                {
                    targetPosition = tapPoints[_currentLayerIndex].point;
                    targetPositionFound = true;
                }
            }

            var nextLayer = (_currentLayerIndex + 1) % 2;
            if (!targetPositionFound &&
                nextLayer < _tilemap.GetLayerCount() &&
                tapPoints.ContainsKey(nextLayer))
            {
                targetPosition = tapPoints[nextLayer].point;
                targetPositionFound = true;
            }

            if (!targetPositionFound)
            {
                targetPosition = tapPoints.First().Value.point;
            }

            var moveMode = isDoubleTap ? 1 : 0;
            // Keep running when actor is already in running mode
            if (_currentPath?.MovementMode == 1) moveMode = 1;
            SetupPath(new[] { targetPosition }, moveMode, EndOfPathActionType.Idle, ignoreObstacle: false);
        }

        private bool IsNearPortalAreaOfLayer(Vector3 position, int layerIndex)
        {
            Vector2Int tilePosition = _tilemap.GetTilePosition(position, layerIndex);

            if (_tilemap.IsInsidePortalArea(tilePosition, layerIndex)) return true;

            // Check nearby 8 directions
            return Enum.GetValues(typeof(Direction)).Cast<Direction>().Any(direction =>
                _tilemap.IsInsidePortalArea(tilePosition + DirectionUtils.ToVector2Int(direction), layerIndex));
        }

        public MovementResult MoveTowards(Vector3 targetPosition, int movementMode, bool ignoreObstacle = false)
        {
            Transform currentTransform = transform;
            Vector3 currentPosition = currentTransform.position;

            // TODO: Use speed info from datascript\scene.txt file when _actor.Info.Speed == 0
            var moveSpeed = _actor.Info.Speed <= 0 ? (movementMode == 1 ? 11f : 5f) : _actor.Info.Speed / 11f;

            if (!_actor.IsMainActor()) moveSpeed /= 2f;

            Vector3 newPosition = Vector3.MoveTowards(currentPosition, targetPosition, moveSpeed * Time.deltaTime);

            // If actor is moving towards a collider, check if the new position is still inside the collider.
            // If yes, don't move the actor. This is to prevent the actor from moving through the collider
            // since we are not moving the rigidbody directly but instead moving the transform.
            if (IsDuringCollision() && _actionController.GetRigidBody() is { isKinematic: false } &&
                IsNewPositionInsideCollisionCollider(currentPosition, newPosition))
            {
                RotateTowards(currentPosition, newPosition, movementMode);
                return MovementResult.InProgress;
            }

            var canGotoPosition = CanGotoPosition(currentPosition, newPosition, out var newYPosition);

            if (!canGotoPosition && !ignoreObstacle)
            {
                return MovementResult.Blocked;
            }

            switch (ignoreObstacle)
            {
                case false when Mathf.Abs(newYPosition - newPosition.y) > MAX_Y_DIFFERENTIAL:
                    return MovementResult.Blocked;
                case true when Mathf.Abs(newYPosition - newPosition.y) > MAX_Y_DIFFERENTIAL:
                    newYPosition = currentPosition.y;
                    break;
            }

            RotateTowards(currentPosition, newPosition, movementMode);

            currentTransform.position = new Vector3(
                newPosition.x,
                newYPosition,
                newPosition.z);

            if (Mathf.Abs(currentTransform.position.x - targetPosition.x) < 0.05f &&
                Mathf.Abs(currentTransform.position.z - targetPosition.z) < 0.05f)
            {
                return MovementResult.Completed;
            }

            return MovementResult.InProgress;
        }

        private void RotateTowards(Vector3 currentPosition, Vector3 targetPosition, int movementMode)
        {
            Transform currentTransform = transform;

            Vector3 moveDirection = new Vector3(
                targetPosition.x - currentPosition.x,
                0f,
                targetPosition.z - currentPosition.z).normalized;

            // Special handling for moving backwards
            if (movementMode == 2)
            {
                currentTransform.forward = moveDirection;
            }
            else
            {
                currentTransform.forward = Vector3.RotateTowards(currentTransform.forward,
                    moveDirection, DEFAULT_ROTATION_SPEED * Time.deltaTime, 0.0f);
            }
        }

        private bool IsNewPositionInsideCollisionCollider(Vector3 currentPosition, Vector3 newPosition)
        {
            // Check if actor is running into any of the active collision colliders
            foreach (ActiveColliderInfo colliderInfo in _activeColliders)
            {
                if (colliderInfo.Collider == null)
                {
                    continue; // In case the collider is destroyed
                }

                var centerYPosition = _actionController.GetRendererBounds().center.y;

                var fromCenterPosition = new Vector3(currentPosition.x, centerYPosition, currentPosition.z);
                var toCenterPosition = new Vector3(newPosition.x, centerYPosition, newPosition.z);
                Vector3 movingDirection = (toCenterPosition - fromCenterPosition).normalized;

                if (_actionController.GetCollider() is { } capsuleCollider)
                {
                    if (Utility.IsPointWithinCollider(colliderInfo.Collider,
                            toCenterPosition + movingDirection * capsuleCollider.radius))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool CanGotoPosition(Vector3 currentPosition, Vector3 newPosition, out float newYPosition)
        {
            newYPosition = 0f;

            // Check if actor is on top of a platform
            if (IsNearOrOnTopOfPlatform() && TryGetNearestActiveStandingPlatform(out ActiveStandingPlatformInfo platformInfo))
            {
                var targetYPosition = platformInfo.Platform.GetPlatformHeight();

                // Make sure actor is on top of the platform
                if (Utility.IsPointWithinCollider(platformInfo.Platform.GetCollider(),
                        new Vector3(newPosition.x, targetYPosition, newPosition.z)) &&
                    Mathf.Abs(currentPosition.y - targetYPosition) <= MAX_Y_DIFFERENTIAL_CROSS_PLATFORM)
                {
                    newYPosition = targetYPosition;
                    return true;
                }
            }

            // New position is not blocked at current layer
            if (_tilemap.TryGetTile(newPosition, _currentLayerIndex, out NavTile tileAtCurrentLayer) &&
                tileAtCurrentLayer.IsWalkable())
            {
                newYPosition = GameBoxInterpreter.ToUnityYPosition(tileAtCurrentLayer.GameBoxYPosition);
                return true;
            }

            if (_tilemap.GetLayerCount() <= 1) return false;

            var nextLayer = (_currentLayerIndex + 1) % 2;

            // New position is not blocked at next layer and y offset is within range
            if (_tilemap.TryGetTile(newPosition, nextLayer, out NavTile tileAtNextLayer) &&
                tileAtNextLayer.IsWalkable())
            {
                var yPositionAtNextLayer = GameBoxInterpreter.ToUnityYPosition(tileAtNextLayer.GameBoxYPosition);

                if (Mathf.Abs(currentPosition.y - yPositionAtNextLayer) > MAX_Y_DIFFERENTIAL_CROSS_LAYER)
                {
                    return false;
                }

                // Switching layer
                SetNavLayer(nextLayer);
                newYPosition = yPositionAtNextLayer;
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

        public void SetupPath(Vector3[] wayPoints,
            int mode,
            EndOfPathActionType endOfPathAction,
            bool ignoreObstacle,
            string specialAction = null)
        {
            _currentPath.SetPath(wayPoints, mode, endOfPathAction, ignoreObstacle);
            _actionController.PerformAction(specialAction ?? _actor.GetMovementAction(mode));
        }

        private void ReachingToEndOfPath()
        {
            _movementWaiter?.CancelWait();

            switch (_currentPath.EndOfPathAction)
            {
                case EndOfPathActionType.DisposeSelf:
                    CommandDispatcher<ICommand>.Instance.Dispatch(new ActorActivateCommand(_actor.Info.Id, 0));
                    break;
                case EndOfPathActionType.Idle:
                    _actionController.PerformAction(_actor.GetIdleAction());
                    break;
                case EndOfPathActionType.WaitAndReverse:
                {
                    _actionController.PerformAction(_actor.GetIdleAction());
                    var waypoints = _currentPath.GetAllWayPoints();
                    waypoints.Reverse();
                    StartCoroutine(WaitForSomeTimeAndFollowPath(waypoints.ToArray(),
                        _currentPath.MovementMode,
                        _movementCts.Token));
                    break;
                }
            }

            // Special handling for final rotation after moving backwards
            if (_currentPath.MovementMode == 2)
            {
                Transform actorTransform = transform;
                actorTransform.forward = -actorTransform.forward;
            }

            _currentPath.Clear();
        }

        private IEnumerator WaitForSomeTimeAndFollowPath(Vector3[] waypoints, int mode, CancellationToken cancellationToken)
        {
            yield return new WaitForSeconds(Random.Range(3, 8));
            yield return new WaitUntil(() => !_isMovementOnHold);
            if (!cancellationToken.IsCancellationRequested)
            {
                SetupPath(waypoints, mode, EndOfPathActionType.WaitAndReverse, ignoreObstacle: true);
            }
        }

        public IEnumerator MoveDirectlyTo(Vector3 position, int mode)
        {
            _currentPath.Clear();
            MovementResult result;
            _actionController.PerformAction(_actor.GetMovementAction(mode));
            do
            {
                result = MoveTowards(position, mode, ignoreObstacle: true);
                yield return null;
            } while (result == MovementResult.InProgress);
            _actionController.PerformAction(_actor.GetIdleAction());
        }

        private IEnumerator FindPathAndMoveToTilePosition(Vector2Int toTilePosition,
            int mode,
            EndOfPathActionType endOfPathAction,
            CancellationToken cancellationToken,
            bool moveTowardsPositionIfNoPathFound = false,
            string specialAction = default)
        {
            Vector2Int fromTilePosition = _tilemap.GetTilePosition(transform.position, _currentLayerIndex);

            if (fromTilePosition == toTilePosition)
            {
                _movementWaiter?.CancelWait();
                yield break;
            }

            Vector2Int[] path = Array.Empty<Vector2Int>();
            var obstacles = _getAllActiveActorBlockingTilePositions(_currentLayerIndex, new [] {(int)_actor.Info.Id});

            var pathFindingThread = new Thread(() =>
            {
                path = _tilemap.FindPathToTilePositionThreadSafe(
                    fromTilePosition,
                    toTilePosition,
                    _currentLayerIndex,
                    obstacles);
            })
            {
                IsBackground = true,
                Priority = System.Threading.ThreadPriority.Highest
            };
            pathFindingThread.Start();

            while (pathFindingThread.IsAlive)
            {
                yield return null;
            }

            if (cancellationToken.IsCancellationRequested) yield break;

            if (path.Length <= 0)
            {
                if (moveTowardsPositionIfNoPathFound)
                {
                    var directWayPoints = new[]
                    {
                        _tilemap.GetWorldPosition(toTilePosition, _currentLayerIndex),
                    };

                    SetupPath(directWayPoints, mode, endOfPathAction, ignoreObstacle: true, specialAction);
                }
                else
                {
                    _movementWaiter?.CancelWait();
                    Debug.LogError($"Failed to find path from tile position {fromTilePosition} " +
                                   $"to tile position: {toTilePosition}");
                }
                yield break;
            }

            var wayPoints = new Vector3[path.Length];
            for (var i = 0; i < path.Length; i++)
            {
                wayPoints[i] = _tilemap.GetWorldPosition(new Vector2Int(path[i].x, path[i].y), _currentLayerIndex);
            }

            SetupPath(wayPoints, mode, endOfPathAction, ignoreObstacle: true, specialAction);
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
            SetupPath(wayPoints, mode, EndOfPathActionType.Idle, ignoreObstacle: false);
        }

        public void Execute(ActorSetWorldPositionCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;

            Vector2Int tilePosition = _tilemap.GetTilePosition(
                new Vector3(command.XPosition, 0f, command.ZPosition), _currentLayerIndex);

            if (_tilemap.IsTilePositionInsideTileMap(tilePosition, _currentLayerIndex))
            {
                Execute(new ActorSetTilePositionCommand(command.ActorId, tilePosition.x, tilePosition.y));
            }
            else // Try next layer
            {
                tilePosition = _tilemap.GetTilePosition(
                    new Vector3(command.XPosition, 0f, command.ZPosition), (_currentLayerIndex + 1) % 2);

                if (_tilemap.IsTilePositionInsideTileMap(tilePosition, (_currentLayerIndex + 1) % 2))
                {
                    Execute(new ActorSetTilePositionCommand(command.ActorId, tilePosition.x, tilePosition.y));
                }
            }
        }

        public void Execute(ActorSetTilePositionCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;

            CancelMovement();

            var tilePosition = new Vector2Int(command.TileXPosition, command.TileYPosition);

            // Check if position at current layer exists,
            // if not, auto switch to next layer (if tile at next layer is walkable)
            if (_tilemap.GetLayerCount() > 1)
            {
                var isTileInsideCurrentLayer = _tilemap.IsTilePositionInsideTileMap(tilePosition, _currentLayerIndex);
                var isTileInsideAtNextLayer = _tilemap.IsTilePositionInsideTileMap(tilePosition, (_currentLayerIndex + 1) % 2);
                if (!isTileInsideCurrentLayer && isTileInsideAtNextLayer)
                {
                    SetNavLayer((_currentLayerIndex + 1) % 2);
                }
            }

            transform.position = _tilemap.GetWorldPosition(tilePosition, _currentLayerIndex);

            // Cancel current action if any
            if (_actionController.GetCurrentAction() != string.Empty)
            {
                _actionController.PerformAction(_actor.GetIdleAction());
            }
        }

        public void Execute(ActorPathToCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            _movementWaiter?.CancelWait();
            _movementWaiter = new WaitUntilCanceled(this);
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerWaitRequest(_movementWaiter));
            StartCoroutine(FindPathAndMoveToTilePosition(new Vector2Int(command.TileXPosition, command.TileYPosition),
                command.Mode, EndOfPathActionType.Idle, _movementCts.Token));
        }

        #if PAL3A
        public void Execute(ActorWalkToUsingActionCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            _movementWaiter?.CancelWait();
            _movementWaiter = new WaitUntilCanceled(this);
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerWaitRequest(_movementWaiter));
            StartCoroutine(FindPathAndMoveToTilePosition(new Vector2Int(command.TileXPosition, command.TileYPosition),
                mode: 0, EndOfPathActionType.Idle, _movementCts.Token, specialAction: command.Action));
        }
        #endif

        public void Execute(ActorMoveToCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            MoveToTilePosition(new Vector2Int(command.TileXPosition, command.TileYPosition), command.Mode);
        }

        public void Execute(ActorMoveBackwardsCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            var moveDistance = GameBoxInterpreter.ToUnityDistance(command.GameBoxDistance);
            Vector3 newPosition = transform.position +  (-transform.forward * moveDistance);
            MoveTo(newPosition, 2);
        }

        public void Execute(ActorMoveOutOfScreenCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;

            _movementWaiter?.CancelWait();
            _movementWaiter = new WaitUntilCanceled(this);
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerWaitRequest(_movementWaiter));

            StartCoroutine(FindPathAndMoveToTilePosition(
                new Vector2Int(command.TileXPosition, command.TileYPosition),
                command.Mode,
                EndOfPathActionType.DisposeSelf,
                _movementCts.Token,
                true));
        }

        public void Execute(ActorStopActionAndStandCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            _movementWaiter?.CancelWait();
            _movementCts?.Cancel();
            _movementCts = new CancellationTokenSource();
            _currentPath.Clear();
        }

        public void Execute(ActorSetNavLayerCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            SetNavLayer(command.LayerIndex);
        }

        public void Execute(ActorActivateCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            if (command.IsActive == 0)
            {
                _movementWaiter?.CancelWait();
                _movementCts?.Cancel();
                _movementCts = new CancellationTokenSource();
                _currentPath.Clear();

                _activeColliders.Clear();
                _activeStandingPlatforms.Clear();
            }
        }
    }
}