// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using System.Collections.Generic;
    using Actor;
    using Common;
    using Core.Animation;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Data;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [ScnSceneObject(ScnSceneObjectType.MovableCarrier)]
    public class MovableCarrierObject : SceneObject
    {
        private float MOVE_SPEED = 3.5f;

        private StandingPlatformController _platformController;

        private bool _isInteractionInProgress;

        public MovableCarrierObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();

            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            Bounds bounds = GetPolyModelRenderer().GetMeshBounds();
            bounds.size *= 1.25f; // Make it a little bigger
            var yOffset = -0.1f;

            _platformController = sceneGameObject.AddComponent<StandingPlatformController>();
            _platformController.SetBounds(bounds, ObjectInfo.LayerIndex, yOffset);
            _platformController.OnPlayerActorEntered += OnPlayerActorEntered;

            // Init position
            sceneGameObject.transform.position = GameBoxInterpreter.ToUnityPosition(ObjectInfo.SwitchState == 0 ?
                ObjectInfo.Path.GameBoxWaypoints[0] :
                ObjectInfo.Path.GameBoxWaypoints[ObjectInfo.Path.NumberOfWaypoints - 1]);

            return sceneGameObject;
        }

        private void OnPlayerActorEntered(object sender, GameObject playerActorGameObject)
        {
            if (_isInteractionInProgress) return;
            _isInteractionInProgress = true;
            RequestForInteraction();
        }

        public override IEnumerator Interact(InteractionContext ctx)
        {
            GameObject carrierObject = GetGameObject();

            // Triggered by other objects
            if (ctx.InitObjectId != ObjectInfo.Id)
            {
                ToggleAndSaveSwitchState();

                carrierObject.transform.position = GameBoxInterpreter.ToUnityPosition(ObjectInfo.SwitchState == 0 ?
                    ObjectInfo.Path.GameBoxWaypoints[0] :
                    ObjectInfo.Path.GameBoxWaypoints[ObjectInfo.Path.NumberOfWaypoints - 1]);

                yield break;
            }

            var platformController = carrierObject.GetComponent<StandingPlatformController>();
            Vector3 platformPosition = platformController.transform.position;
            var actorStandingPosition = new Vector3(
                platformPosition.x,
                platformController.GetPlatformHeight(),
                platformPosition.z);

            var actorMovementController = ctx.PlayerActorGameObject.GetComponent<ActorMovementController>();

            yield return actorMovementController.MoveDirectlyTo(actorStandingPosition, 0);

            var waypoints = new List<Vector3>();
            for (int i = 0; i < ObjectInfo.Path.NumberOfWaypoints; i++)
            {
                Vector3 waypoint = GameBoxInterpreter.ToUnityPosition(ObjectInfo.Path.GameBoxWaypoints[i]);
                if (ObjectInfo.SwitchState == 0)
                {
                    waypoints.Add(waypoint);
                }
                else
                {
                    waypoints.Insert(0, waypoint);
                }
            }

            Transform carrierObjectTransform = carrierObject.transform;

            for (int i = 1; i < waypoints.Count; i++)
            {
                var duration = Vector3.Distance(waypoints[i], carrierObjectTransform.position) / MOVE_SPEED;
                yield return AnimationHelper.MoveTransform(carrierObjectTransform,
                    waypoints[i],
                    duration,
                    AnimationCurveType.Linear);
            }

            ToggleAndSaveSwitchState();

            Vector3 lastSectionForwardVector = (waypoints[^1] - waypoints[^2]).normalized;
            lastSectionForwardVector.y = 0f;
            Bounds bounds = platformController.GetCollider().bounds;

            Vector3 actorFinalPosition = ctx.PlayerActorGameObject.transform.position + lastSectionForwardVector *
              (Mathf.Sqrt(bounds.size.x * bounds.size.x + bounds.size.z * bounds.size.z) / 2f + 1.5f);

            yield return actorMovementController.MoveDirectlyTo(actorFinalPosition, 0);

            _isInteractionInProgress = false;
        }

        public override void Deactivate()
        {
            if (_platformController != null)
            {
                _platformController.OnPlayerActorEntered -= OnPlayerActorEntered;
                Object.Destroy(_platformController);
            }

            base.Deactivate();
        }
    }
}