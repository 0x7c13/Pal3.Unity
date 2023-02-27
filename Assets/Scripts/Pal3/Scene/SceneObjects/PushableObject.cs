// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

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
        private const float PUSH_ANIMATION_DURATION = 2f;

        private BoundsTriggerController _triggerController;

        private readonly SceneStateManager _sceneStateManager;

        private bool _isInteractionInProgress;

        private int _bidirectionalPushableCurrentState;
        private int _bidirectionalPushableGoalState;

        public PushableObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _sceneStateManager = ServiceLocator.Instance.Get<SceneStateManager>();
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();

            #if PAL3
            // Temporarily disable pushable boxes in PAL3 m02 3 scene.
            // TODO: proper impl
            if (SceneInfo.Is("m02", "3"))
            {
                return new GameObject($"Object_{ObjectInfo.Id}_{ObjectInfo.Type}_Placeholder");
            }
            #endif

            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            _triggerController = sceneGameObject.AddComponent<BoundsTriggerController>();
            _triggerController.SetupCollider(GetMeshBounds(), false);
            _triggerController.OnPlayerActorEntered += OnPlayerActorEntered;

            return sceneGameObject;
        }

        private void OnPlayerActorEntered(object sender, GameObject playerGameObject)
        {
            if (_isInteractionInProgress) return;
            _isInteractionInProgress = true;

            GameObject pushableObject = GetGameObject();
            Transform playerActorTransform =  playerGameObject.transform;
            Transform pushableObjectTransform = pushableObject.transform;

            Bounds bounds = GetMeshBounds();
            float movingDistance = (bounds.size.x + bounds.size.z) / 2f;

            Vector3 relativeDirection = pushableObjectTransform.position - playerActorTransform.position;
            relativeDirection.y = 0f; // Ignore y axis
            Vector3 movingDirection = GetClosetMovableDirection(relativeDirection);

            if (IsTargetDirectionValid(pushableObjectTransform.position, movingDirection, movingDistance))
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
            List<Vector3> validDirections;

            if (ObjectInfo.Parameters[0] == 2)
            {
                var forwardDirection = GetGameObject().transform.forward;
                validDirections = new List<Vector3>()
                {
                    forwardDirection,
                    -forwardDirection,
                };
            }
            else
            {
                var forwardDirection = GetGameObject().transform.forward;
                var rightDirection = GetGameObject().transform.right;
                validDirections = new List<Vector3>()
                {
                    forwardDirection,
                    -forwardDirection,
                    rightDirection,
                    -rightDirection,
                };
            }

            float smallestAngle = float.MaxValue;
            Vector3 closetDirection = Vector3.forward;

            foreach (Vector3 direction in validDirections)
            {
                float facingAngle = Vector3.Angle(direction, vector);
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
            yield return actorMovementController.MoveDirectlyToAsync(actorHoldingPosition, 0, true);

            playerActorTransform.forward = movingDirection;
            var actorActionController = ctx.PlayerActorGameObject.GetComponent<ActorActionController>();
            actorActionController.PerformAction(ActorConstants.ActionNames[ActorActionType.Push],
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
                switch (_bidirectionalPushableGoalState)
                {
                    case -1:
                        yield return ActivateOrInteractWithObjectIfAnyAsync(ctx, (ushort)ObjectInfo.Parameters[1]);
                        break;
                    case 1:
                        yield return ActivateOrInteractWithObjectIfAnyAsync(ctx, (ushort)ObjectInfo.Parameters[2]);
                        break;
                    case 0:
                        switch (_bidirectionalPushableCurrentState)
                        {
                            case -1:
                                yield return ActivateOrInteractWithObjectIfAnyAsync(ctx, (ushort)ObjectInfo.Parameters[1]);
                                break;
                            case 1:
                                yield return ActivateOrInteractWithObjectIfAnyAsync(ctx, (ushort)ObjectInfo.Parameters[2]);
                                break;
                        }
                        break;
                }

                _bidirectionalPushableCurrentState = 0;
                _bidirectionalPushableGoalState = 0;
            }

            if (ObjectInfo.ScriptId == ScriptConstants.InvalidScriptId)
            {
                SaveCurrentPosition();
            }

            _isInteractionInProgress = false;
        }

        private bool IsTargetDirectionValid(Vector3 objectPosition, Vector3 direction, float distance)
        {
            if (ObjectInfo.Parameters[0] == 2)
            {
                if (_sceneStateManager.TryGetSceneObjectStateOverride(SceneInfo.CityName,
                        SceneInfo.SceneName, ObjectInfo.Id, out SceneObjectStateOverride state) &&
                    state.BidirectionalPushableObjectState.HasValue)
                {
                    switch (state.BidirectionalPushableObjectState.Value)
                    {
                        case 1:
                            if (GetGameObject().transform.forward == direction)
                            {
                                return false;
                            }
                            break;
                        case -1:
                            if (GetGameObject().transform.forward == -direction)
                            {
                                return false;
                            }
                            break;
                    }

                    _bidirectionalPushableCurrentState = state.BidirectionalPushableObjectState.Value;
                }

                _bidirectionalPushableGoalState = _bidirectionalPushableCurrentState
                                                  + (GetGameObject().transform.forward == direction ? 1 : -1);

                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new SceneSaveGlobalBidirectionalPushableObjectStateCommand(
                        SceneInfo.CityName,
                        SceneInfo.SceneName,
                        ObjectInfo.Id,
                        _bidirectionalPushableGoalState));

                return true;
            }
            else
            {

            }

            return true;
        }

        public override void Deactivate()
        {
            _bidirectionalPushableCurrentState = 0;
            _bidirectionalPushableGoalState = 0;
            _isInteractionInProgress = false;

            if (_triggerController != null)
            {
                _triggerController.OnPlayerActorEntered -= OnPlayerActorEntered;
                Object.Destroy(_triggerController);
            }

            base.Deactivate();
        }
    }
}