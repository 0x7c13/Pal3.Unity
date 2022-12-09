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
            var directions = new List<Vector3>()
            {
                Vector3.left,
                Vector3.right,
                Vector3.forward,
                Vector3.back,
            };

            float smallestAngle = float.MaxValue;
            Vector3 closetDirection = Vector3.forward;

            foreach (Vector3 direction in directions)
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

            Vector3 relativeDirection = pushableObjectTransform.position - playerActorTransform.position;
            relativeDirection.y = 0f; // Ignore y axis
            Vector3 movingDirection = GetClosetMovableDirection(relativeDirection);

            var actorMovementController = ctx.PlayerActorGameObject.GetComponent<ActorMovementController>();
            actorMovementController.CancelMovement();
            playerActorTransform.forward = movingDirection;
            var actorActionController = ctx.PlayerActorGameObject.GetComponent<ActorActionController>();
            actorActionController.PerformAction(ActorConstants.ActionNames[ActorActionType.Push],
                overwrite: true, loopCount: -1);

            Bounds bounds = GetMeshBounds();
            float movingDistance = (bounds.size.x + bounds.size.z) / 2f;

            Vector3 actorInitPosition = playerActorTransform.position;
            Vector3 objectInitPosition = pushableObjectTransform.position;

            PlaySfx("we025", 3);

            yield return AnimationHelper.EnumerateValue(0f, movingDistance, PUSH_ANIMATION_DURATION,
                AnimationCurveType.Linear, value =>
                {
                    pushableObjectTransform.position = objectInitPosition + movingDirection * value;
                    playerActorTransform.position = actorInitPosition + movingDirection * value;
                });

            actorActionController.PerformAction(ActorActionType.Stand);

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