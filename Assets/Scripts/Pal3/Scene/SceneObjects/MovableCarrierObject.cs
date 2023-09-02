// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Actor.Controllers;
    using Command;
    using Command.SceCommands;
    using Common;
    using Core.Animation;
    using Core.Contracts;
    using Core.DataReader.Scn;
    using Core.Extensions;
    using Core.GameBox;
    using Data;
    using UnityEngine;

    [ScnSceneObject(SceneObjectType.MovableCarrier)]
    public sealed class MovableCarrierObject : SceneObject
    {
        private const float MOVE_SPEED = 5f;

        private StandingPlatformController _platformController;

        private bool _isInteractionInProgress;

        public MovableCarrierObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameObject();

            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            Bounds bounds = GetMeshBounds();
            bounds.size += new Vector3(0.6f, 0.2f, 0.6f); // Extend the bounds a little bit

            #if PAL3
            if (SceneInfo.Is("m23", "5") &&
                ObjectInfo.Name.Equals("_d.pol", StringComparison.OrdinalIgnoreCase))
            {
                bounds = new Bounds
                {
                    center = bounds.center,
                    size = new Vector3(8.5f, 13.5f, 8.5f),
                };
            }
            #elif PAL3A
            if (SceneInfo.Is("m10", "2") &&
                (ObjectInfo.Name.Equals("_f.pol", StringComparison.OrdinalIgnoreCase) ||
                 ObjectInfo.Name.Equals("_g.pol", StringComparison.OrdinalIgnoreCase)))
            {
                bounds = new Bounds
                {
                    center = new Vector3(0f, -0.3f, 0f),
                    size = new Vector3(6f, 0.7f, 6f),
                };
            }
            #endif

            _platformController = sceneGameObject.AddComponent<StandingPlatformController>();
            _platformController.Init(bounds, ObjectInfo.LayerIndex);
            _platformController.OnPlayerActorEntered += OnPlayerActorEntered;

            return sceneGameObject;
        }

        private void OnPlayerActorEntered(object sender, GameObject playerActorGameObject)
        {
            if (_isInteractionInProgress) return;

            // Make sure player is at the same height as the platform
            if (Mathf.Abs(playerActorGameObject.transform.position.y -
                          _platformController.GetPlatformHeight()) <= 0.5f)
            {
                _isInteractionInProgress = true;
                RequestForInteraction();
            }
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            #if PAL3
            if (SceneInfo.Is("m23", "5") &&
                ObjectInfo.Name.Equals("_d.pol", StringComparison.OrdinalIgnoreCase))
            {
                // TODO: Remove this and implement the correct logic
                CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand("中途不停车哦~"));
            }
            #endif

            GameObject carrierObject = GetGameObject();
            var isNearFirstWaypoint = IsNearFirstWaypoint();

            // Triggered by other objects
            if (ctx.InitObjectId != ObjectInfo.Id)
            {
                carrierObject.transform.position = (isNearFirstWaypoint ?
                    ObjectInfo.Path.GameBoxWaypoints[ObjectInfo.Path.NumberOfWaypoints - 1] :
                    ObjectInfo.Path.GameBoxWaypoints[0]).ToUnityPosition();

                SaveCurrentPosition();
                yield break;
            }
            else
            {
                #if PAL3A
                // PAL3A has additional interaction logic for grouped objects
                // This is to sync moving carriers in m07-2 scene
                if (SceneInfo.Is("m07", "2") &&
                    ObjectInfo.LinkedObjectGroupId != 0)
                {
                    var allObjects = ctx.CurrentScene.GetAllSceneObjects();
                    foreach (SceneObject otherObject in allObjects.Values)
                    {
                        if (ObjectInfo.Id != otherObject.ObjectInfo.Id &&
                            ObjectInfo.Type == otherObject.ObjectInfo.Type &&
                            ObjectInfo.LinkedObjectGroupId == otherObject.ObjectInfo.LinkedObjectGroupId &&
                            otherObject.IsActivated)
                        {
                            yield return otherObject.InteractAsync(ctx);
                        }

                    }
                }
                #endif
            }

            var actorMovementController = ctx.PlayerActorGameObject.GetComponent<ActorMovementController>();
            var waypoints = new List<Vector3>();

            // Move player to center of the carrier
            {
                Vector3 platformPosition = _platformController.transform.position;
                var actorStandingPosition = new Vector3(
                    platformPosition.x,
                    _platformController.GetPlatformHeight(),
                    platformPosition.z);

                yield return actorMovementController.MoveDirectlyToAsync(actorStandingPosition, 0, true);
            }

            // Move carrier to final waypoint
            {
                for (var i = 0; i < ObjectInfo.Path.NumberOfWaypoints; i++)
                {
                    Vector3 waypoint = ObjectInfo.Path.GameBoxWaypoints[i].ToUnityPosition();
                    if (isNearFirstWaypoint)
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
                    yield return carrierObjectTransform.MoveAsync(waypoints[i],
                        duration);
                }
            }

            Bounds bounds = _platformController.GetCollider().bounds;
            Vector3 actorPositionOnCarrier = ctx.PlayerActorGameObject.transform.position;

            // Move actor out of carrier to the final standing position
            {
                Vector3 lastSectionForwardVector = (waypoints[^1] - waypoints[^2]).normalized;
                lastSectionForwardVector.y = 0f;

                Vector3 actorFinalPosition = actorPositionOnCarrier + lastSectionForwardVector *
                    (Mathf.Sqrt(bounds.size.x * bounds.size.x + bounds.size.z * bounds.size.z) / 2f + 1.5f);

                // Do not ignore obstacles when moving out of the carrier in PAL3 scene m17 7
                // since the final position might be blocked by a lifted platform
                #if PAL3
                bool ignoreObstacle = !SceneInfo.Is("m17", "7");
                #else
                bool ignoreObstacle = true;
                #endif

                yield return actorMovementController.MoveDirectlyToAsync(actorFinalPosition, 0, ignoreObstacle);

                // Verify if actor is able to move to final position,
                // if so, end the interaction
                if (Vector2.Distance(
                        new Vector2(actorFinalPosition.x, actorFinalPosition.z),
                        new Vector2(ctx.PlayerActorGameObject.transform.position.x,
                            ctx.PlayerActorGameObject.transform.position.z)) < 0.1f)
                {
                    SaveCurrentPosition();
                    _isInteractionInProgress = false;
                    yield break;
                }
            }

            // Actor is not able to move to final position, meaning it is blocked by something.
            // Move the actor back to carrier. And move the carrier back to original position.
            {
                yield return actorMovementController.MoveDirectlyToAsync(actorPositionOnCarrier, 0, true);

                waypoints.Reverse(); // Reverse the waypoints
                Transform carrierObjectTransform = carrierObject.transform;

                for (int i = 1; i < waypoints.Count; i++)
                {
                    var duration = Vector3.Distance(waypoints[i], carrierObjectTransform.position) / MOVE_SPEED;
                    yield return carrierObjectTransform.MoveAsync(waypoints[i],
                        duration);
                }

                Vector3 lastSectionForwardVector = (waypoints[^1] - waypoints[^2]).normalized;
                lastSectionForwardVector.y = 0f;

                actorPositionOnCarrier = ctx.PlayerActorGameObject.transform.position;
                Vector3 actorFinalPosition = actorPositionOnCarrier + lastSectionForwardVector *
                    (Mathf.Sqrt(bounds.size.x * bounds.size.x + bounds.size.z * bounds.size.z) / 2f + 1.4f);

                yield return actorMovementController.MoveDirectlyToAsync(actorFinalPosition, 0, true);
            }

            SaveCurrentPosition();
            _isInteractionInProgress = false;
        }

        private bool IsNearFirstWaypoint()
        {
            Vector3 gameBoxPosition = GetGameObject().transform.position.ToGameBoxPosition();
            Vector3 firstWaypoint = ObjectInfo.Path.GameBoxWaypoints[0];
            Vector3 lastWaypoint = ObjectInfo.Path.GameBoxWaypoints[ObjectInfo.Path.NumberOfWaypoints - 1];
            return Vector3.Distance(gameBoxPosition, firstWaypoint) < Vector3.Distance(gameBoxPosition, lastWaypoint);
        }

        public override void Deactivate()
        {
            if (_platformController != null)
            {
                _platformController.OnPlayerActorEntered -= OnPlayerActorEntered;
                _platformController.Destroy();
                _platformController = null;
            }

            base.Deactivate();
        }
    }
}