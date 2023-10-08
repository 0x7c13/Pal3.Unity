// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Actor.Controllers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Enums;
    using Core.DataReader.Nav;
    using Core.Primitives;
    using Core.Utilities;
    using Engine.Abstraction;
    using Engine.Extensions;
    using Engine.Logging;
    using Engine.Navigation;
    using Engine.Services;
    using Engine.Utilities;
    using Scene;
    using Scene.SceneObjects.Common;
    using Script.Waiter;
    using UnityEngine;

    public enum MovementResult
    {
        InProgress,
        Blocked,
        Completed,
    }

    internal sealed class ActiveColliderInfo
    {
        public Collider Collider;
        public Vector3 ActorPositionWhenCollisionEnter;
    }

    internal sealed class ActiveStandingPlatformInfo
    {
        public StandingPlatformController Platform;
        public Vector3 PlatformLastKnownPosition;
        public double TimeWhenEntered;
    }

    public class ActorMovementController : TickableGameEntityScript,
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
        private IGameTimeProvider _gameTimeProvider;

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

        private float _movementMaxYDifferential;
        private float _movementMaxYDifferentialCrossLayer;
        private float _movementMaxYDifferentialCrossPlatform;

        private Func<int, int[], HashSet<Vector2Int>> _getAllActiveActorBlockingTilePositions;

        protected override void OnEnableGameEntity()
        {
            _gameTimeProvider = ServiceLocator.Instance.Get<IGameTimeProvider>();
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        protected override void OnDisableGameEntity()
        {
            _currentPath.Clear();
            _movementWaiter?.CancelWait();
            _movementCts?.Cancel();
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Init(Actor actor,
            Tilemap tilemap,
            ActorActionController actionController,
            float movementMaxYDifferential,
            float movementMaxYDifferentialCrossLayer,
            float movementMaxYDifferentialCrossPlatform,
            Func<int, int[], HashSet<Vector2Int>> getAllActiveActorBlockingTilePositions)
        {
            _actor = actor;
            _tilemap = tilemap;
            _actionController = actionController;
            _currentLayerIndex = actor.Info.LayerIndex;
            _movementMaxYDifferential = movementMaxYDifferential;
            _movementMaxYDifferentialCrossLayer = movementMaxYDifferentialCrossLayer;
            _movementMaxYDifferentialCrossPlatform = movementMaxYDifferentialCrossPlatform;
            _getAllActiveActorBlockingTilePositions = getAllActiveActorBlockingTilePositions;

            Vector3 initPosition = new GameBoxVector3(
                actor.Info.GameBoxXPosition,
                actor.Info.GameBoxYPosition,
                actor.Info.GameBoxZPosition).ToUnityPosition();

            // Setup actor's initial position
            if (actor.Info.InitBehaviour != ActorBehaviourType.Hold &&
                _tilemap.TryGetTile(initPosition, _currentLayerIndex, out NavTile tile))
            {
                // If the actor is initially placed on a walkable tile, snap to the tile
                // Same for the case when actor is configured to not facing the player actor during dialogue
                if (tile.IsWalkable() || actor.Info.NoTurn == 1)
                {
                    Transform.Position = new Vector3(initPosition.x,
                        tile.GameBoxYPosition.ToUnityYPosition(),
                        initPosition.z);
                }
                else // This is to handle the case where the actor is initially placed on a non-walkable tile
                {
                    Vector2Int tilePosition = _tilemap.GetTilePosition(initPosition, _currentLayerIndex);
                    // Snap to the nearest adjacent tile if exists
                    var hasAdjacentWalkableTile =_tilemap.TryGetAdjacentWalkableTile(tilePosition,
                        _currentLayerIndex, out Vector2Int nearestTile);
                    Transform.Position = hasAdjacentWalkableTile ?
                        _tilemap.GetWorldPosition(nearestTile, _currentLayerIndex) :
                        initPosition;
                }
            }
            else
            {
                Transform.Position = initPosition;
            }
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

        private bool TryGetLastEnteredStandingPlatform(out ActiveStandingPlatformInfo platformInfo)
        {
            platformInfo = null;
            var latestTime = double.MinValue;

            foreach (ActiveStandingPlatformInfo info in _activeStandingPlatforms)
            {
                if (info.Platform == null)
                {
                    continue; // In case the platform is destroyed
                }

                if (info.TimeWhenEntered > latestTime)
                {
                    latestTime = info.TimeWhenEntered;
                    platformInfo = info;
                }
            }

            return platformInfo != null;
        }

        private Vector3 GetNearestLastKnownPositionWhenCollisionEnter()
        {
            Vector3 nearestPosition = Transform.Position;
            float distance = float.MaxValue;

            foreach (var colliderInfo in _activeColliders)
            {
                var distanceToLastKnownPosition = Vector3.Distance(
                    colliderInfo.ActorPositionWhenCollisionEnter, Transform.Position);
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

        protected override void OnUpdateGameEntity(float deltaTime)
        {
            if (_isMovementOnHold || _currentPath.IsEndOfPath()) return;

            MovementResult result = MoveTowards(_currentPath.GetCurrentWayPoint(),
                _currentPath.MovementMode,
                _currentPath.IgnoreObstacle,
                _actor.GetMovementSpeed(_currentPath.MovementMode) * deltaTime,
                _actor.GetRotationSpeed() * deltaTime);

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

        protected override void OnLateUpdateGameEntity(float deltaTime)
        {
            // To sync with the platform movement
            if (IsNearOrOnTopOfPlatform() && TryGetLastEnteredStandingPlatform(
                    out ActiveStandingPlatformInfo platformInfo))
            {
                Vector3 currentPlatformPosition = platformInfo.Platform.Transform.Position;

                if (currentPlatformPosition != platformInfo.PlatformLastKnownPosition)
                {
                    Transform.Position += currentPlatformPosition - platformInfo.PlatformLastKnownPosition;
                    platformInfo.PlatformLastKnownPosition = currentPlatformPosition;
                }
            }
        }

        protected override void OnFixedUpdateGameEntity(float fixedDeltaTime)
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
                ITransform actorTransform = Transform;
                Vector3 currentPosition = actorTransform.Position;

                if (IsNearOrOnTopOfPlatform())
                {
                    // Do nothing.
                }
                else if (!_tilemap.TryGetTile(currentPosition, _currentLayerIndex, out NavTile tile))
                {
                    actorTransform.Position = GetNearestLastKnownPositionWhenCollisionEnter();
                }
                else
                {
                    if (!tile.IsWalkable())
                    {
                        actorTransform.Position = GetNearestLastKnownPositionWhenCollisionEnter();
                    }
                    else
                    {
                        var adjustedPosition = new Vector3(currentPosition.x,
                            tile.GameBoxYPosition.ToUnityYPosition(),
                            currentPosition.z);
                        actorTransform.Position = adjustedPosition;
                    }
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            Vector3 currentActorPosition = Transform.Position;
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
                        PlatformLastKnownPosition = triggerCollider.gameObject.transform.position,
                        TimeWhenEntered = _gameTimeProvider.RealTimeSinceStartup,
                    });

                // Move actor on to the platform if platform is higher than current position
                Vector3 currentPosition = Transform.Position;
                var targetYPosition = standingPlatformController.GetPlatformHeight();
                if (MathF.Abs(currentPosition.y - targetYPosition) <= _movementMaxYDifferentialCrossPlatform &&
                    currentPosition.y < targetYPosition) // Don't move the actor if platform is lower
                {
                    Transform.Position = new Vector3(currentPosition.x, targetYPosition, currentPosition.z);
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
            return Transform.Position;
        }

        public Vector2Int GetTilePosition()
        {
            return _tilemap.GetTilePosition(Transform.Position, _currentLayerIndex);
        }

        public void PortalToPosition(Vector3 position, int layerIndex, bool isPositionOnStandingPlatform)
        {
            if (isPositionOnStandingPlatform)
            {
                _currentPath?.Clear();
                _actionController.PerformAction(_actor.GetIdleAction());
                SetNavLayer(layerIndex);
                Transform.Position = position;

                EngineLogger.LogWarning($"Portal to standing platform: {position}, " +
                                 $"layer: {layerIndex}");
            }
            else if (_tilemap.TryGetTile(position, layerIndex, out NavTile tile) && tile.IsWalkable())
            {
                _currentPath?.Clear();
                _actionController.PerformAction(_actor.GetIdleAction());
                SetNavLayer(layerIndex);
                Transform.Position = new Vector3(
                    position.x,
                    tile.GameBoxYPosition.ToUnityYPosition(),
                    position.z);

                Vector2Int tilePosition = _tilemap.GetTilePosition(position, layerIndex);
                EngineLogger.LogWarning($"Portal to: {position}, " +
                                 $"layer: {layerIndex}, " +
                                 $"tile: {tilePosition}, " +
                                 $"DistanceToNearestObstacle: {tile.DistanceToNearestObstacle}, " +
                                 $"FloorKind: {tile.FloorType}");
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
                tapPoints.TryGetValue(nextLayer, out (Vector3 point, bool isPlatform) tapPoint))
            {
                targetPosition = tapPoint.point;
                targetPositionFound = true;
            }

            if (!targetPositionFound)
            {
                targetPosition = tapPoints.First().Value.point;
            }

            MovementMode movementMode = isDoubleTap ? MovementMode.Run : MovementMode.Walk;

            // Keep running when actor is already in running mode regardless of the tap behavior
            if (_currentPath?.MovementMode == MovementMode.Run) movementMode = MovementMode.Run;

            SetupPath(new[] { targetPosition }, movementMode, EndOfPathActionType.Idle, ignoreObstacle: false);
        }

        public MovementResult MoveTowards(Vector3 targetPosition,
            MovementMode movementMode,
            bool ignoreObstacle,
            float maxDistanceDelta,
            float maxRadiansDelta)
        {
            Vector3 currentPosition = Transform.Position;

            var currentXZPosition = new Vector2(currentPosition.x, currentPosition.z);
            var targetXZPosition = new Vector2(targetPosition.x, targetPosition.z);

            Vector2 goalXZPosition = Vector2.MoveTowards(currentXZPosition, targetXZPosition, maxDistanceDelta);

            float goalYPosition = MathF.Abs(targetPosition.y - currentPosition.y) < 0.01f ? targetPosition.y :
                (Vector2.Distance(goalXZPosition, currentXZPosition) /
                 Vector2.Distance(targetXZPosition, currentXZPosition)) *
                 (targetPosition.y - currentPosition.y) + currentPosition.y;

            var newPosition = new Vector3(goalXZPosition.x, goalYPosition, goalXZPosition.y);

            // If actor is moving towards a collider, check if the new position is still inside the collider.
            // If yes, don't move the actor. This is to prevent the actor from moving through the collider
            // since we are not moving the rigidbody directly but instead moving the transform.
            if (IsDuringCollision() && _actionController.GetRigidBody() is { isKinematic: false } &&
                IsNewPositionInsideCollisionCollider(currentPosition, newPosition))
            {
                RotateTowards(currentPosition, newPosition, movementMode, maxRadiansDelta);
                return MovementResult.InProgress;
            }

            var canGotoPosition = CanGotoPosition(currentPosition, newPosition, out var newYPosition);

            if (!canGotoPosition && !ignoreObstacle)
            {
                return MovementResult.Blocked;
            }

            switch (ignoreObstacle)
            {
                case false when MathF.Abs(newYPosition - newPosition.y) > _movementMaxYDifferential:
                    return MovementResult.Blocked;
                case true when MathF.Abs(newYPosition - newPosition.y) > _movementMaxYDifferential:
                    newYPosition = currentPosition.y;
                    break;
            }

            RotateTowards(currentPosition, newPosition, movementMode, maxRadiansDelta);

            ITransform actorTransform = Transform;
            actorTransform.Position = new Vector3(
                newPosition.x,
                newYPosition,
                newPosition.z);

            if (MathF.Abs(actorTransform.Position.x - targetPosition.x) < 0.05f &&
                MathF.Abs(actorTransform.Position.z - targetPosition.z) < 0.05f)
            {
                return MovementResult.Completed;
            }

            return MovementResult.InProgress;
        }

        private void RotateTowards(Vector3 currentPosition,
            Vector3 targetPosition,
            MovementMode movementMode,
            float maxRadiansDelta)
        {
            Vector3 moveDirection = new Vector3(
                targetPosition.x - currentPosition.x,
                0f,
                targetPosition.z - currentPosition.z).normalized;

            // Special handling for stepping backwards
            if (movementMode == MovementMode.StepBack)
            {
                Transform.Forward = moveDirection;
            }
            else
            {
                Transform.Forward = Vector3.RotateTowards(Transform.Forward,
                    moveDirection, maxRadiansDelta, 0.0f);
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
                    if (UnityEngineUtility.IsPointInsideCollider(colliderInfo.Collider,
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
            newYPosition = float.MinValue;
            var isNewPositionValid = false;

            // Check if actor is on top of a platform
            if (IsNearOrOnTopOfPlatform())
            {
                // Check if the new position is still on top of the nearest platform(s)
                foreach (ActiveStandingPlatformInfo platformInfo in _activeStandingPlatforms)
                {
                    var targetYPosition = platformInfo.Platform.GetPlatformHeight();

                    // Tolerance is used to allow the actor to be able to walk
                    // on a standing platform that has a slight slope
                    const float tolerance = 0.3f;

                    // Make sure actor is on top of the platform
                    if (UnityEngineUtility.IsPointInsideCollider(platformInfo.Platform.GetCollider(),
                            new Vector3(newPosition.x, targetYPosition, newPosition.z), tolerance) &&
                        MathF.Abs(currentPosition.y - targetYPosition) <= _movementMaxYDifferentialCrossPlatform)
                    {
                        newYPosition = targetYPosition;
                        isNewPositionValid = true;
                        break;
                    }
                }
            }

            // New position is not blocked at current layer
            if (_tilemap.TryGetTile(newPosition, _currentLayerIndex, out NavTile tileAtCurrentLayer) &&
                tileAtCurrentLayer.IsWalkable())
            {
                var tileYPosition = tileAtCurrentLayer.GameBoxYPosition.ToUnityYPosition();

                // Choose a higher position if possible
                if (tileYPosition > newYPosition)
                {
                    newYPosition = tileYPosition;
                }

                isNewPositionValid = true;
            }

            if (isNewPositionValid) return true;

            if (_tilemap.GetLayerCount() <= 1) return false;

            var nextLayer = (_currentLayerIndex + 1) % 2;

            // New position is not blocked at next layer and y offset is within range
            if (_tilemap.TryGetTile(newPosition, nextLayer, out NavTile tileAtNextLayer) &&
                tileAtNextLayer.IsWalkable())
            {
                var yPositionAtNextLayer = tileAtNextLayer.GameBoxYPosition.ToUnityYPosition();

                if (MathF.Abs(currentPosition.y - yPositionAtNextLayer) > _movementMaxYDifferentialCrossLayer)
                {
                    return false;
                }

                // Switching layer
                SetNavLayer(nextLayer);
                newYPosition = yPositionAtNextLayer;
                return true;
            }

            // Special handling for moving between layers (Across portal area)
            if (_tilemap.IsPositionInsidePortalArea(newPosition, _currentLayerIndex) ||
                _tilemap.IsPositionInsidePortalArea(newPosition, nextLayer))
            {
                newYPosition = currentPosition.y;
                return true;
            }

            return false;
        }

        public void SetupPath(Vector3[] wayPoints,
            MovementMode movementMode,
            EndOfPathActionType endOfPathAction,
            bool ignoreObstacle,
            string specialAction = null)
        {
            _currentPath.SetPath(wayPoints, movementMode, endOfPathAction, ignoreObstacle);
            _actionController.PerformAction(specialAction ?? _actor.GetMovementAction(movementMode));
        }

        private void ReachingToEndOfPath()
        {
            _movementWaiter?.CancelWait();

            switch (_currentPath.EndOfPathAction)
            {
                case EndOfPathActionType.DisposeSelf:
                    CommandDispatcher<ICommand>.Instance.Dispatch(new ActorActivateCommand(_actor.Id, 0));
                    break;
                case EndOfPathActionType.Idle:
                    _actionController.PerformAction(_actor.GetIdleAction());
                    break;
                case EndOfPathActionType.WaitAndReverse:
                {
                    _actionController.PerformAction(_actor.GetIdleAction());
                    Vector3[] waypoints = _currentPath.GetAllWayPoints();
                    Array.Reverse(waypoints);
                    StartCoroutine(WaitForSomeTimeAndFollowPathAsync(waypoints,
                        _currentPath.MovementMode,
                        _movementCts.Token));
                    break;
                }
            }

            // Special handling for final rotation after stepping backwards
            if (_currentPath.MovementMode == MovementMode.StepBack)
            {
                Transform.Forward = -Transform.Forward;
            }

            _currentPath.Clear();
        }

        private IEnumerator WaitForSomeTimeAndFollowPathAsync(Vector3[] waypoints,
            MovementMode movementMode,
            CancellationToken cancellationToken)
        {
            yield return new WaitForSeconds(RandomGenerator.Range(3f, 8f));
            yield return new WaitUntil(() => !_isMovementOnHold);
            if (!cancellationToken.IsCancellationRequested)
            {
                SetupPath(waypoints, movementMode, EndOfPathActionType.WaitAndReverse, ignoreObstacle: true);
            }
        }

        public IEnumerator MoveDirectlyToAsync(Vector3 position, MovementMode movementMode, bool ignoreObstacle)
        {
            _currentPath.Clear();
            MovementResult result;
            _actionController.PerformAction(_actor.GetMovementAction(movementMode));
            do
            {
                Vector3 currentPosition = Transform.Position;
                var deltaTime = _gameTimeProvider.DeltaTime;

                result = MoveTowards(position,
                    movementMode,
                    ignoreObstacle,
                    _actor.GetMovementSpeed(_currentPath.MovementMode) * deltaTime,
                    _actor.GetRotationSpeed() * deltaTime);

                yield return null;

                Vector3 newPosition = Transform.Position;
                if (result == MovementResult.InProgress &&
                    currentPosition == newPosition)
                {
                    // No movement, gets blocked by collider
                    break;
                }
            } while (result == MovementResult.InProgress);
            _actionController.PerformAction(_actor.GetIdleAction());
        }

        private IEnumerator FindPathAndMoveToTilePositionAsync(Vector2Int toTilePosition,
            MovementMode movementMode,
            EndOfPathActionType endOfPathAction,
            CancellationToken cancellationToken,
            bool moveTowardsPositionIfNoPathFound = false,
            string specialAction = default)
        {
            Vector2Int fromTilePosition = _tilemap.GetTilePosition(Transform.Position, _currentLayerIndex);

            if (fromTilePosition == toTilePosition)
            {
                _movementWaiter?.CancelWait();
                yield break;
            }

            Vector2Int[] path = Array.Empty<Vector2Int>();
            var obstacles = _getAllActiveActorBlockingTilePositions(_currentLayerIndex, new [] {_actor.Id});

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

                    SetupPath(directWayPoints, movementMode, endOfPathAction, ignoreObstacle: true, specialAction);
                }
                else
                {
                    _movementWaiter?.CancelWait();
                    EngineLogger.LogError($"Failed to find path from tile position {fromTilePosition} " +
                                   $"to tile position: {toTilePosition}");
                }
                yield break;
            }

            var wayPoints = new Vector3[path.Length];
            for (var i = 0; i < path.Length; i++)
            {
                wayPoints[i] = _tilemap.GetWorldPosition(new Vector2Int(path[i].x, path[i].y), _currentLayerIndex);
            }

            SetupPath(wayPoints, movementMode, endOfPathAction, ignoreObstacle: true, specialAction);
        }

        private void MoveToTilePosition(Vector2Int position, MovementMode movementMode)
        {
            MoveTo(_tilemap.GetWorldPosition(position, _currentLayerIndex), movementMode);
        }

        private void MoveTo(Vector3 position, MovementMode movementMode)
        {
            _movementWaiter?.CancelWait();
            _movementWaiter = new WaitUntilCanceled();
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(_movementWaiter));

            var wayPoints = new [] { position };
            SetupPath(wayPoints, movementMode, EndOfPathActionType.Idle, ignoreObstacle: false);
        }

        public void Execute(ActorSetWorldPositionCommand command)
        {
            if (_actor.Id != command.ActorId) return;

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
            if (_actor.Id != command.ActorId) return;

            CancelMovement();

            var tilePosition = new Vector2Int(command.TileXPosition, command.TileYPosition);

            // Check if position at current layer exists, if not, auto switch
            // to next layer (if tile at next layer is valid).
            // Also, switch to next layer if tile at current layer is obstacle.
            if (_tilemap.GetLayerCount() > 1)
            {
                bool isTileExistsInCurrentLayer = _tilemap.TryGetTile(tilePosition,
                    _currentLayerIndex, out NavTile tileAtCurrentLayer);
                bool isTileExistsInNextLayer = _tilemap.TryGetTile(tilePosition,
                    (_currentLayerIndex + 1) % 2, out NavTile tileAtNextLayer);

                if (!isTileExistsInCurrentLayer && isTileExistsInNextLayer)
                {
                    SetNavLayer((_currentLayerIndex + 1) % 2);
                }
                else if (isTileExistsInCurrentLayer && isTileExistsInNextLayer &&
                         tileAtCurrentLayer.DistanceToNearestObstacle == 0 &&
                         tileAtNextLayer.DistanceToNearestObstacle > 0)
                    // DO NOT USE IsWalkable() to check if tile is obstacle, because it can be
                    // overwritten by script (Ex: Tilemap.MarkFloorKindAsObstacle())
                {
                    SetNavLayer((_currentLayerIndex + 1) % 2);
                }
            }

            Transform.Position = _tilemap.GetWorldPosition(tilePosition, _currentLayerIndex);

            // Cancel current action if any
            if (_actionController.GetCurrentAction() != string.Empty)
            {
                _actionController.PerformAction(_actor.GetIdleAction());
            }
        }

        public void Execute(ActorPathToCommand command)
        {
            if (_actor.Id != command.ActorId) return;
            _movementWaiter?.CancelWait();
            _movementWaiter = new WaitUntilCanceled();
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(_movementWaiter));
            StartCoroutine(FindPathAndMoveToTilePositionAsync(new Vector2Int(command.TileXPosition, command.TileYPosition),
                (MovementMode)command.Mode, EndOfPathActionType.Idle, _movementCts.Token));
        }

        #if PAL3A
        public void Execute(ActorWalkToUsingActionCommand command)
        {
            if (_actor.Id != command.ActorId) return;
            _movementWaiter?.CancelWait();
            _movementWaiter = new WaitUntilCanceled();
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(_movementWaiter));
            StartCoroutine(FindPathAndMoveToTilePositionAsync(new Vector2Int(command.TileXPosition, command.TileYPosition),
                movementMode: 0, EndOfPathActionType.Idle, _movementCts.Token, specialAction: command.Action));
        }
        #endif

        public void Execute(ActorMoveToCommand command)
        {
            if (_actor.Id != command.ActorId) return;
            MoveToTilePosition(new Vector2Int(command.TileXPosition, command.TileYPosition), (MovementMode)command.Mode);
        }

        public void Execute(ActorMoveBackwardsCommand command)
        {
            if (_actor.Id != command.ActorId) return;
            var moveDistance = command.GameBoxDistance.ToUnityDistance();
            Vector3 newPosition = Transform.Position + (-Transform.Forward * moveDistance);
            MoveTo(newPosition, MovementMode.StepBack);
        }

        public void Execute(ActorMoveOutOfScreenCommand command)
        {
            if (_actor.Id != command.ActorId) return;

            _movementWaiter?.CancelWait();
            _movementWaiter = new WaitUntilCanceled();
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(_movementWaiter));

            StartCoroutine(FindPathAndMoveToTilePositionAsync(
                new Vector2Int(command.TileXPosition, command.TileYPosition),
                (MovementMode)command.Mode,
                EndOfPathActionType.DisposeSelf,
                _movementCts.Token,
                true));
        }

        public void Execute(ActorStopActionAndStandCommand command)
        {
            if (_actor.Id != command.ActorId) return;
            _movementWaiter?.CancelWait();
            _movementCts?.Cancel();
            _movementCts = new CancellationTokenSource();
            _currentPath.Clear();
        }

        public void Execute(ActorSetNavLayerCommand command)
        {
            if (_actor.Id != command.ActorId) return;
            SetNavLayer(command.LayerIndex);
        }

        public void Execute(ActorActivateCommand command)
        {
            if (_actor.Id != command.ActorId) return;
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