// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Actor.Controllers;
    using Command;
    using Common;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Data;
    using Engine.Animation;
    using Engine.Core.Abstraction;
    using Engine.Extensions;

    using Bounds = UnityEngine.Bounds;
    using Color = Core.Primitives.Color;
    using Vector2 = UnityEngine.Vector2;
    using Vector3 = UnityEngine.Vector3;

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

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();

            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

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

            _platformController = sceneObjectGameEntity.AddComponent<StandingPlatformController>();
            _platformController.Init(bounds, ObjectInfo.LayerIndex);
            _platformController.OnPlayerActorEntered += OnPlayerActorEntered;

            return sceneObjectGameEntity;
        }

        private void OnPlayerActorEntered(object sender, IGameEntity playerActorGameEntity)
        {
            if (_isInteractionInProgress) return;

            // Make sure player is at the same height as the platform
            if (MathF.Abs(playerActorGameEntity.Transform.Position.y -
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
                Pal3.Instance.Execute(new UIDisplayNoteCommand("中途不停车哦~"));
            }
            #endif

            IGameEntity carrierEntity = GetGameEntity();
            bool isNearFirstWaypoint = IsNearFirstWaypoint();

            // Triggered by other objects
            if (ctx.InitObjectId != ObjectInfo.Id)
            {
                carrierEntity.Transform.Position = (isNearFirstWaypoint ?
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
                    Dictionary<int, SceneObject> allObjects = ctx.CurrentScene.GetAllSceneObjects();
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

            ActorMovementController actorMovementController = ctx.PlayerActorGameEntity.GetComponent<ActorMovementController>();
            List<Vector3> waypoints = new();

            // Move player to center of the carrier
            {
                Vector3 platformPosition = _platformController.Transform.Position;
                Vector3 actorStandingPosition = new(
                    platformPosition.x,
                    _platformController.GetPlatformHeight(),
                    platformPosition.z);

                yield return actorMovementController.MoveDirectlyToAsync(actorStandingPosition, 0, true);
            }

            // Move carrier to final waypoint
            {
                for (int i = 0; i < ObjectInfo.Path.NumberOfWaypoints; i++)
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

                ITransform carrierObjectTransform = carrierEntity.Transform;

                for (int i = 1; i < waypoints.Count; i++)
                {
                    float duration = Vector3.Distance(waypoints[i], carrierObjectTransform.Position) / MOVE_SPEED;
                    yield return carrierObjectTransform.MoveAsync(waypoints[i],
                        duration);
                }
            }

            Bounds bounds = _platformController.GetCollider().bounds;
            Vector3 actorPositionOnCarrier = ctx.PlayerActorGameEntity.Transform.Position;

            // Move actor out of carrier to the final standing position
            {
                Vector3 lastSectionForwardVector = (waypoints[^1] - waypoints[^2]).normalized;
                lastSectionForwardVector.y = 0f;

                Vector3 actorFinalPosition = actorPositionOnCarrier + lastSectionForwardVector *
                    (MathF.Sqrt(bounds.size.x * bounds.size.x + bounds.size.z * bounds.size.z) / 2f + 1.5f);

                // Do not ignore obstacles when moving out of the carrier in
                // PAL3 scene m17-7 and PAL3A scene m11-4 (锁妖塔4层)
                // since the final position might be blocked by a lifted platform
                #if PAL3
                bool ignoreObstacle = !SceneInfo.Is("m17", "7");
                #elif PAL3A
                bool ignoreObstacle = !SceneInfo.Is("m11", "4");
                #else
                bool ignoreObstacle = true;
                #endif

                yield return actorMovementController.MoveDirectlyToAsync(actorFinalPosition, 0, ignoreObstacle);

                // Verify if actor is able to move to final position,
                // if so, end the interaction
                if (Vector2.Distance(
                        new Vector2(actorFinalPosition.x, actorFinalPosition.z),
                        new Vector2(ctx.PlayerActorGameEntity.Transform.Position.x,
                            ctx.PlayerActorGameEntity.Transform.Position.z)) < 0.1f)
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
                ITransform carrierObjectTransform = carrierEntity.Transform;

                for (int i = 1; i < waypoints.Count; i++)
                {
                    float duration = Vector3.Distance(waypoints[i], carrierObjectTransform.Position) / MOVE_SPEED;
                    yield return carrierObjectTransform.MoveAsync(waypoints[i],
                        duration);
                }

                Vector3 lastSectionForwardVector = (waypoints[^1] - waypoints[^2]).normalized;
                lastSectionForwardVector.y = 0f;

                actorPositionOnCarrier = ctx.PlayerActorGameEntity.Transform.Position;
                Vector3 actorFinalPosition = actorPositionOnCarrier + lastSectionForwardVector *
                    (MathF.Sqrt(bounds.size.x * bounds.size.x + bounds.size.z * bounds.size.z) / 2f + 1.4f);

                yield return actorMovementController.MoveDirectlyToAsync(actorFinalPosition, 0, true);
            }

            SaveCurrentPosition();
            _isInteractionInProgress = false;
        }

        private bool IsNearFirstWaypoint()
        {
            Vector3 position = GetGameEntity().Transform.Position;
            Vector3 firstWaypoint = ObjectInfo.Path.GameBoxWaypoints[0].ToUnityPosition();
            Vector3 lastWaypoint = ObjectInfo.Path.GameBoxWaypoints[ObjectInfo.Path.NumberOfWaypoints - 1].ToUnityPosition();
            return Vector3.Distance(position, firstWaypoint) < Vector3.Distance(position, lastWaypoint);
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