// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Actor;
    using Common;
    using Core.Animation;
    using Core.DataReader.Scn;
    using Data;
    using MetaData;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [ScnSceneObject(ScnSceneObjectType.Pushable)]
    public sealed class PushableObject : SceneObject
    {
        private const float PUSH_ANIMATION_DURATION = 2.5f;

        private BoundsTriggerController _triggerController;

        private bool _isInteractionInProgress;

        public PushableObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
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
            RequestForInteraction();
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

        public override IEnumerator Interact(InteractionContext ctx)
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
            yield return actorMovementController.MoveDirectlyTo(actorHoldingPosition, 0, true);

            playerActorTransform.forward = movingDirection;
            var actorActionController = ctx.PlayerActorGameObject.GetComponent<ActorActionController>();
            actorActionController.PerformAction(ActorConstants.ActionNames[ActorActionType.Push],
                overwrite: true, loopCount: -1);

            Vector3 actorInitPosition = playerActorTransform.position;
            Vector3 objectInitPosition = pushableObjectTransform.position;

            PlaySfx("we025", 3);

            yield return AnimationHelper.EnumerateValue(0f, movingDistance, PUSH_ANIMATION_DURATION,
                AnimationCurveType.Linear, value =>
                {
                    pushableObjectTransform.position = objectInitPosition + movingDirection * value;
                    playerActorTransform.position = actorInitPosition + movingDirection * value;
                });

            // Move player actor back a bit to avoid collision with the pushable object again.
            Vector3 actorFinalPosition = actorInitPosition + movingDirection * (movingDistance - 1f);
            yield return actorMovementController.MoveDirectlyTo(actorFinalPosition, 0, true);

            yield return ExecuteScriptAndWaitForFinishIfAny();

            _isInteractionInProgress = false;
        }

        public override void Deactivate()
        {
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