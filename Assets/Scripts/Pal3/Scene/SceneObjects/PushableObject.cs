// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Actor;
    using Command;
    using Command.InternalCommands;
    using Common;
    using Core.Animation;
    using Core.DataReader.Nav;
    using Core.DataReader.Scn;
    using Core.Services;
    using Data;
    using MetaData;
    using State;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [ScnSceneObject(ScnSceneObjectType.Pushable)]
    public sealed class PushableObject : SceneObject
    {
        private const float PUSH_ANIMATION_DURATION = 1.7f;

        private BoundsTriggerController _triggerController;
        private StandingPlatformController _standingPlatformController;

        private readonly SceneStateManager _sceneStateManager;
        private readonly Tilemap _tilemap;

        private bool _isInteractionInProgress;

        private int _bidiPushableCurrentState;
        private int _bidiPushableGoalState;

        private readonly RaycastHit[] _raycastHits = new RaycastHit[10];

        public PushableObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _sceneStateManager = ServiceLocator.Instance.Get<SceneStateManager>();
            _tilemap = ServiceLocator.Instance.Get<SceneManager>().GetCurrentScene().GetTilemap();
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();

            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

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

            _triggerController = sceneGameObject.AddComponent<BoundsTriggerController>();
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
                    Vector3 currentPosition = sceneGameObject.transform.position;
                    if (Math.Abs(currentPosition.x - (-49.04671f)) < 0.01f)
                    {
                        sceneGameObject.transform.position = new Vector3(-47.5f,
                            currentPosition.y,
                            currentPosition.z);
                    }
                    platformBounds.center = new Vector3(0f, 2.8f, 0f);
                    platformBounds.size = new Vector3(1.3f, 0.7f, 3.5f);
                }

                _standingPlatformController = sceneGameObject.AddComponent<StandingPlatformController>();
                _standingPlatformController.Init(platformBounds, ObjectInfo.LayerIndex, -0.4f);
            }

            return sceneGameObject;
        }

        private void OnPlayerActorEntered(object sender, GameObject playerGameObject)
        {
            GameObject pushableObject = GetGameObject();

            // Don't allow player to push object if it's not on the same "layer"
            if (Math.Abs(playerGameObject.transform.position.y - pushableObject.transform.position.y) > 1f)
            {
                return;
            }

            if (_isInteractionInProgress) return;
            _isInteractionInProgress = true;

            Vector3 playerActorPosition =  playerGameObject.transform.position;
            Vector3 pushableObjectPosition = pushableObject.transform.position;

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
            GameObject pushableObject = GetGameObject();

            // Bi-directional pushable object
            if (ObjectInfo.Parameters[0] == 2)
            {
                Vector3 forwardDirection = pushableObject.transform.forward;
                validPushingDirections = new List<Vector3>()
                {
                    forwardDirection,
                    -forwardDirection,
                };
            }
            else // Normal pushable object
            {
                Vector3 forwardDirection = pushableObject.transform.forward;
                Vector3 rightDirection = pushableObject.transform.right;
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

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            if (!IsInteractableBasedOnTimesCount()) yield break;

            GameObject pushableObject = GetGameObject();
            Transform playerActorTransform =  ctx.PlayerActorGameObject.transform;
            Transform pushableObjectTransform = pushableObject.transform;

            Bounds bounds = GetMeshBounds();
            float movingDistance = (bounds.size.x + bounds.size.z) / 2f;

            Vector3 relativeDirection = pushableObjectTransform.position - playerActorTransform.position;
            relativeDirection.y = 0f; // Ignore y axis
            Vector3 movingDirection = GetClosetMovableDirection(relativeDirection);

            // Move player actor to holding position
            var actorMovementController = ctx.PlayerActorGameObject.GetComponent<ActorMovementController>();
            actorMovementController.CancelMovement();
            Vector3 actorHoldingPosition = pushableObjectTransform.position + -movingDirection * (movingDistance * 0.8f);
            actorHoldingPosition.y = playerActorTransform.position.y;
            yield return actorMovementController.MoveDirectlyToAsync(actorHoldingPosition, 0, false);

            Vector3 actorCurrentPosition = actorMovementController.GetWorldPosition();
            if (Vector2.Distance(new Vector2(actorHoldingPosition.x, actorHoldingPosition.z)
                    , new Vector2(actorCurrentPosition.x, actorCurrentPosition.z)) > 0.1f)
            {
                _isInteractionInProgress = false;
                yield break; // Actor cannot reach the holding position, abort.
            }

            playerActorTransform.forward = movingDirection;
            var actorActionController = ctx.PlayerActorGameObject.GetComponent<ActorActionController>();
            actorActionController.PerformAction(ActorConstants.ActionToNameMap[ActorActionType.Push],
                overwrite: true, loopCount: -1);

            Vector3 actorInitPosition = playerActorTransform.position;
            Vector3 objectInitPosition = pushableObjectTransform.position;

            PlaySfx("we025", 3);

            yield return AnimationHelper.EnumerateValueAsync(0f, movingDistance, PUSH_ANIMATION_DURATION,
                AnimationCurveType.Linear, value =>
                {
                    pushableObjectTransform.position = objectInitPosition + movingDirection * value;
                    playerActorTransform.position = actorInitPosition + movingDirection * value;
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
            GameObject pushableObject = GetGameObject();

            // Bi-directional pushable object
            if (ObjectInfo.Parameters[0] == 2)
            {
                if (_sceneStateManager.TryGetSceneObjectStateOverride(SceneInfo.CityName,
                        SceneInfo.SceneName, ObjectInfo.Id, out SceneObjectStateOverride state) &&
                    state.BidirectionalPushableObjectState.HasValue)
                {
                    Vector3 pushableObjectForwardDirection = pushableObject.transform.forward;

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
                                         (pushableObject.transform.forward == direction ? 1 : -1);

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
                Vector3 targetPosition = pushableObject.transform.position + direction * distance;
                if (!_tilemap.TryGetTile(targetPosition, ObjectInfo.LayerIndex, out NavTile tile) ||
                    !tile.IsWalkable())
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsDirectionBlockedByOtherObjects(Vector3 direction, float distanceCheckRange)
        {
            GameObject pushableObject = GetGameObject();

            Bounds meshBounds = GetMeshBounds();
            Bounds rendererBounds = GetRendererBounds();

            var hitCount = Physics.BoxCastNonAlloc(rendererBounds.center,
                new Vector3(meshBounds.size.x / 2f * 0.8f,
                    meshBounds.size.y / 2f * 0.8f,  // 0.8f is to make sure the boxcast is
                    meshBounds.size.z / 2f * 0.8f), // smaller than the mesh for tolerance
                direction,
                _raycastHits,
                Quaternion.LookRotation(direction));

            int raycastOnlyLayer = LayerMask.NameToLayer("RaycastOnly");

            if (hitCount > 0)
            {
                for (var i = 0; i < hitCount; i++)
                {
                    GameObject colliderObject = _raycastHits[i].collider.gameObject;

                    // Ignore raycast only objects (NavMesh in this case) or self
                    if (colliderObject.layer == raycastOnlyLayer ||
                        colliderObject.gameObject == pushableObject)
                    {
                        continue;
                    }

                    // Can't push if there is an object in front of it within the distance
                    if (Vector3.Distance(pushableObject.transform.position,
                            colliderObject.transform.position) < distanceCheckRange)
                    {
                        return true;
                    }
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
                Object.Destroy(_triggerController);
            }

            if (_standingPlatformController != null)
            {
                Object.Destroy(_standingPlatformController);
            }

            base.Deactivate();
        }
    }
}

#endif