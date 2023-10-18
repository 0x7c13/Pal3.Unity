// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Game.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Actor.Controllers;
    using Command;
    using Command.Extensions;
    using Common;
    using Core.Command;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Nav;
    using Core.DataReader.Scn;
    using Data;
    using Engine.Animation;
    using Engine.Core.Abstraction;
    using Engine.Extensions;
    using Engine.Services;
    using State;

    using Bounds = UnityEngine.Bounds;
    using Color = Core.Primitives.Color;
    using Quaternion = UnityEngine.Quaternion;
    using Vector2 = UnityEngine.Vector2;
    using Vector3 = UnityEngine.Vector3;

    [ScnSceneObject(SceneObjectType.Pushable)]
    public sealed class PushableObject : SceneObject
    {
        private const float PUSH_ANIMATION_DURATION = 1.7f;

        private BoundsTriggerController _triggerController;
        private StandingPlatformController _standingPlatformController;

        private readonly SceneStateManager _sceneStateManager;
        private readonly Tilemap _tilemap;
        private readonly IPhysicsManager _physicsManager;

        private bool _isInteractionInProgress;

        private int _bidiPushableCurrentState;
        private int _bidiPushableGoalState;

        public PushableObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _sceneStateManager = ServiceLocator.Instance.Get<SceneStateManager>();
            _tilemap = ServiceLocator.Instance.Get<SceneManager>().GetCurrentScene().GetTilemap();
            _physicsManager = ServiceLocator.Instance.Get<IPhysicsManager>();
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();

            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            Bounds meshBounds = GetMeshBounds();

            // For pushable object player can stand on
            // Boxes in PAL3 M24-3 scene
            if (ObjectInfo.Parameters[0] == 1)
            {
                meshBounds = new Bounds
                {
                    center = new Vector3(0f, 1.1f, 0f),
                    size = new Vector3(2.5f, 2f, 2.5f),
                };
            }

            _triggerController = sceneObjectGameEntity.AddComponent<BoundsTriggerController>();
            _triggerController.SetBounds(meshBounds, false);
            _triggerController.OnPlayerActorEntered += OnPlayerActorEntered;

            // For pushable object player can stand on
            // Boxes in PAL3 M24-3 scene
            if (ObjectInfo.Parameters[0] == 1)
            {
                Bounds platformBounds = new()
                {
                    center = new Vector3(0f, 2.8f, 0f),
                    size = new Vector3(3.5f, 0.7f, 1.3f),
                };

                // Hack for object 13 in PAL3 M24-3 scene,
                // to block player from walking over the box without pushing it
                if (ObjectInfo.Id == 13)
                {
                    Vector3 currentPosition = sceneObjectGameEntity.Transform.Position;
                    if (MathF.Abs(currentPosition.x - (-49.04671f)) < 0.01f)
                    {
                        sceneObjectGameEntity.Transform.Position = new Vector3(-47.5f,
                            currentPosition.y,
                            currentPosition.z);
                    }
                    platformBounds.center = new Vector3(0f, 2.8f, 0f);
                    platformBounds.size = new Vector3(1.3f, 0.7f, 3.5f);
                }

                _standingPlatformController = sceneObjectGameEntity.AddComponent<StandingPlatformController>();
                _standingPlatformController.Init(platformBounds, ObjectInfo.LayerIndex, -0.4f);
            }

            return sceneObjectGameEntity;
        }

        private void OnPlayerActorEntered(object sender, IGameEntity playerActorGameEntity)
        {
            IGameEntity pushableEntity = GetGameEntity();

            // Don't allow player to push object if it's not on the same "layer"
            if (MathF.Abs(playerActorGameEntity.Transform.Position.y - pushableEntity.Transform.Position.y) > 1f)
            {
                return;
            }

            if (_isInteractionInProgress) return;
            _isInteractionInProgress = true;

            Vector3 playerActorPosition = playerActorGameEntity.Transform.Position;
            Vector3 pushableObjectPosition = pushableEntity.Transform.Position;

            Bounds bounds = GetMeshBounds();
            float movingDistance = (bounds.size.x + bounds.size.z) / 2f;

            Vector3 relativeDirection = pushableObjectPosition - playerActorPosition;
            relativeDirection.y = 0f; // Ignore y axis
            Vector3 movingDirection = GetClosetMovableDirection(relativeDirection);

            Vector3 actorHoldingPosition = pushableObjectPosition + -movingDirection * (movingDistance * 0.8f);
            actorHoldingPosition.y = playerActorPosition.y;

            // Only allow player to push object if actor is close enough
            // to the holding position and there's no obstacle in the way.
            if (Vector3.Distance(playerActorPosition, actorHoldingPosition) < 1f &&
                CanPushTo(movingDirection, movingDistance))
            {
                RequestForInteraction();
            }
            else
            {
                _isInteractionInProgress = false;
            }
        }

        private Vector3 GetClosetMovableDirection(Vector3 vector)
        {
            List<Vector3> validPushingDirections;
            IGameEntity pushableEntity = GetGameEntity();

            // Bi-directional pushable object
            if (ObjectInfo.Parameters[0] == 2)
            {
                Vector3 forwardDirection = pushableEntity.Transform.Forward;
                validPushingDirections = new List<Vector3>()
                {
                    forwardDirection,
                    -forwardDirection,
                };
            }
            else // Normal pushable object
            {
                Vector3 forwardDirection = pushableEntity.Transform.Forward;
                Vector3 rightDirection = pushableEntity.Transform.Right;
                validPushingDirections = new List<Vector3>()
                {
                    forwardDirection,
                    -forwardDirection,
                    rightDirection,
                    -rightDirection,
                };
            }

            var smallestAngle = float.MaxValue;
            Vector3 closetDirection = validPushingDirections[0];

            foreach (Vector3 direction in validPushingDirections)
            {
                var facingAngle = Vector3.Angle(direction, vector);
                if (facingAngle < smallestAngle)
                {
                    smallestAngle = facingAngle;
                    closetDirection = direction;
                }
            }

            return closetDirection;
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            if (!IsInteractableBasedOnTimesCount()) yield break;

            IGameEntity pushableEntity = GetGameEntity();
            ITransform playerActorTransform =  ctx.PlayerActorGameEntity.Transform;
            ITransform pushableObjectTransform = pushableEntity.Transform;

            Bounds bounds = GetMeshBounds();
            float movingDistance = (bounds.size.x + bounds.size.z) / 2f;

            Vector3 relativeDirection = pushableObjectTransform.Position - playerActorTransform.Position;
            relativeDirection.y = 0f; // Ignore y axis
            Vector3 movingDirection = GetClosetMovableDirection(relativeDirection);

            // Move player actor to holding position
            var actorMovementController = ctx.PlayerActorGameEntity.GetComponent<ActorMovementController>();
            actorMovementController.CancelMovement();
            Vector3 actorHoldingPosition = pushableObjectTransform.Position + -movingDirection * (movingDistance * 0.8f);
            actorHoldingPosition.y = playerActorTransform.Position.y;
            yield return actorMovementController.MoveDirectlyToAsync(actorHoldingPosition, 0, false);

            Vector3 actorCurrentPosition = actorMovementController.GetWorldPosition();
            if (Vector2.Distance(new Vector2(actorHoldingPosition.x, actorHoldingPosition.z)
                    , new Vector2(actorCurrentPosition.x, actorCurrentPosition.z)) > 0.1f)
            {
                _isInteractionInProgress = false;
                yield break; // Actor cannot reach the holding position, abort.
            }

            playerActorTransform.Forward = movingDirection;
            var actorActionController = ctx.PlayerActorGameEntity.GetComponent<ActorActionController>();
            actorActionController.PerformAction(ActorConstants.ActionToNameMap[ActorActionType.Push],
                overwrite: true, loopCount: -1);

            Vector3 actorInitPosition = playerActorTransform.Position;
            Vector3 objectInitPosition = pushableObjectTransform.Position;

            PlaySfx("we025", 3);

            yield return CoreAnimation.EnumerateValueAsync(0f, movingDistance, PUSH_ANIMATION_DURATION,
                AnimationCurveType.Linear, value =>
                {
                    pushableObjectTransform.Position = objectInitPosition + movingDirection * value;
                    playerActorTransform.Position = actorInitPosition + movingDirection * value;
                });

            // Move player actor back a bit to avoid collision with the pushable object again.
            Vector3 actorFinalPosition = actorInitPosition + movingDirection * (movingDistance - 1f);
            yield return actorMovementController.MoveDirectlyToAsync(actorFinalPosition, 0, true);

            yield return ExecuteScriptAndWaitForFinishIfAnyAsync();

            // Trigger script based on bidirectional pushable object state
            if (ObjectInfo.Parameters[0] == 2)
            {
                ushort leftLinkedObjectId = (ushort)ObjectInfo.Parameters[1];
                ushort rightLinkedObjectId = (ushort)ObjectInfo.Parameters[2];

                yield return _bidiPushableGoalState switch
                {
                    -1 => ActivateOrInteractWithObjectIfAnyAsync(ctx, leftLinkedObjectId),
                    1 => ActivateOrInteractWithObjectIfAnyAsync(ctx, rightLinkedObjectId),
                    0 when _bidiPushableCurrentState == -1 => ActivateOrInteractWithObjectIfAnyAsync(ctx, leftLinkedObjectId),
                    0 when _bidiPushableCurrentState == 1 => ActivateOrInteractWithObjectIfAnyAsync(ctx, rightLinkedObjectId),
                    _ => throw new NotSupportedException($"Unsupported bidirectional pushable state: {_bidiPushableGoalState}")
                };

                _bidiPushableCurrentState = 0;
                _bidiPushableGoalState = 0;
            }

            // Do not persist position if the object is triggered by script
            // or if the pushable object is in m02.
            if (ObjectInfo.ScriptId == ScriptConstants.InvalidScriptId &&
                !SceneInfo.IsCity("m02"))
            {
                SaveCurrentPosition();
            }

            _isInteractionInProgress = false;
        }

        private bool CanPushTo(Vector3 direction, float distance)
        {
            IGameEntity pushableEntity = GetGameEntity();

            // Bi-directional pushable object
            if (ObjectInfo.Parameters[0] == 2)
            {
                if (_sceneStateManager.TryGetSceneObjectStateOverride(SceneInfo.CityName,
                        SceneInfo.SceneName, ObjectInfo.Id, out SceneObjectStateOverride state) &&
                    state.BidirectionalPushableObjectState.HasValue)
                {
                    Vector3 pushableObjectForwardDirection = pushableEntity.Transform.Forward;

                    bool canPush = state.BidirectionalPushableObjectState.Value switch
                    {
                        1 when pushableObjectForwardDirection == direction => false,
                        -1 when pushableObjectForwardDirection == -direction => false,
                        _ => true
                    };
                    if (!canPush) return false;

                    _bidiPushableCurrentState = state.BidirectionalPushableObjectState.Value;
                }

                _bidiPushableGoalState = _bidiPushableCurrentState +
                                         (pushableEntity.Transform.Forward == direction ? 1 : -1);

                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new SceneSaveGlobalBidirectionalPushableObjectStateCommand(
                        SceneInfo.CityName,
                        SceneInfo.SceneName,
                        ObjectInfo.Id,
                        _bidiPushableGoalState));

                return true;
            }
            else // Normal pushable object
            {
                // Check if target position is blocked by another object or not
                // distance x 2 because we need to consider the size of the other
                // pushable object as well
                if (IsDirectionBlockedByOtherObjects(direction, distance * 2f)) return false;

                // Also check if other direction has blocker or not since player
                // needs to walk to the pushing position first
                if (IsDirectionBlockedByOtherObjects(-direction, distance)) return false;

                // Ignore the vertical raycast check for boxes in PAL3 M24-3 scene
                if (ObjectInfo.Parameters[0] != 1)
                {
                    // Check if there is any pushable object on top of the current one
                    if (IsDirectionBlockedByOtherObjects(Vector3.up, distance * 2f)) return false;

                    // Check if there is any pushable object below the current one
                    if (IsDirectionBlockedByOtherObjects(Vector3.down, distance * 2f)) return false;
                }

                // Check if target position is within the scene bounds
                Vector3 targetPosition = pushableEntity.Transform.Position + direction * distance;
                if (!_tilemap.TryGetTile(targetPosition, ObjectInfo.LayerIndex, out NavTile tile) ||
                    !tile.IsWalkable())
                {
                    return false;
                }
            }

            return true;
        }

        private readonly (Vector3 hitPoint, IGameEntity colliderGameEntity)[] _hitResults = new (Vector3, IGameEntity)[10];
        private bool IsDirectionBlockedByOtherObjects(Vector3 direction, float distanceCheckRange)
        {
            IGameEntity pushableEntity = GetGameEntity();

            Bounds meshBounds = GetMeshBounds();
            Bounds rendererBounds = GetRendererBounds();

            int hitCount = _physicsManager.BoxCast(rendererBounds.center,
                halfExtents: new Vector3(meshBounds.size.x / 2f * 0.8f,
                    meshBounds.size.y / 2f * 0.8f,  // 0.8f is to make sure the boxcast is
                    meshBounds.size.z / 2f * 0.8f), // smaller than the mesh for tolerance
                direction: direction,
                orientation: Quaternion.LookRotation(direction),
                _hitResults);

            for (var i = 0; i < hitCount; i++)
            {
                (Vector3 _, IGameEntity colliderGameEntity) = _hitResults[i];

                // Ignore NavMesh, Actors and self
                if (colliderGameEntity.GetComponent<NavMesh>() != null ||
                    colliderGameEntity.GetComponent<ActorController>() != null ||
                    colliderGameEntity.Name.Equals(pushableEntity.Name, StringComparison.Ordinal))
                {
                    continue;
                }

                // Can't push if there is an object in front of it within the distance
                if (Vector3.Distance(pushableEntity.Transform.Position,
                        colliderGameEntity.Transform.Position) < distanceCheckRange)
                {
                    return true;
                }
            }

            return false;
        }

        public override void Deactivate()
        {
            _bidiPushableCurrentState = 0;
            _bidiPushableGoalState = 0;
            _isInteractionInProgress = false;

            if (_triggerController != null)
            {
                _triggerController.OnPlayerActorEntered -= OnPlayerActorEntered;
                _triggerController.Destroy();
                _triggerController = null;
            }

            if (_standingPlatformController != null)
            {
                _standingPlatformController.Destroy();
                _standingPlatformController = null;
            }

            base.Deactivate();
        }
    }
}

#endif