// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Actor
{
    using System;
    using System.Collections.Generic;
    using Controllers;
    using Core.Contracts;
    using Data;
    using Dev.Presenters;
    using Scene;
    using UnityEngine;

    public static class ActorFactory
    {
        public static GameObject CreateActorGameObject(GameResourceProvider resourceProvider,
            Actor actor,
            Tilemap tilemap,
            bool isDropShadowEnabled,
            Color tintColor,
            float movementMaxYDifferential,
            float movementMaxYDifferentialCrossLayer,
            float movementMaxYDifferentialCrossPlatform,
            Func<int, int[], HashSet<Vector2Int>> getAllActiveActorBlockingTilePositions)
        {
            var actorGameObject = new GameObject($"Actor_{actor.Id}_{actor.Name}")
            {
                layer = LayerMask.NameToLayer("Ignore Raycast")
            };

            // Attach ScnNpcInfo to the GameObject for better debuggability
            #if UNITY_EDITOR
            var npcInfoPresent = actorGameObject.AddComponent<NpcInfoPresenter>();
            npcInfoPresent.npcInfo = actor.Info;
            #endif

            #if PAL3
            var hasColliderAndRigidBody = (PlayerActorId) actor.Id != PlayerActorId.HuaYing;
            #elif PAL3A
            var hasColliderAndRigidBody = (PlayerActorId) actor.Id != PlayerActorId.TaoZi;
            #endif

            ActorActionController actionController;
            switch (actor.AnimationType)
            {
                case ActorAnimationType.Vertex:
                {
                    var autoStand = actor.Info.InitBehaviour != ActorBehaviourType.Hold;
                    var canPerformHoldAnimation = actor.Info is {InitBehaviour: ActorBehaviourType.Hold, LoopAction: 0};
                    VertexAnimationActorActionController vertexActionController =
                        actorGameObject.AddComponent<VertexAnimationActorActionController>();
                    vertexActionController.Init(resourceProvider, actor, hasColliderAndRigidBody, isDropShadowEnabled,
                        autoStand, canPerformHoldAnimation, tintColor);
                    actionController = vertexActionController;
                    break;
                }
                case ActorAnimationType.Skeletal:
                {
                    SkeletalAnimationActorActionController skeletalActionController =
                        actorGameObject.AddComponent<SkeletalAnimationActorActionController>();
                    skeletalActionController.Init(resourceProvider, actor, hasColliderAndRigidBody, isDropShadowEnabled, tintColor);
                    actionController = skeletalActionController;
                    break;
                }
                default:
                    throw new NotSupportedException($"Unsupported actor animation type: {actor.AnimationType}");
            }

            var movementController = actorGameObject.AddComponent<ActorMovementController>();
            movementController.Init(actor,
                tilemap,
                actionController,
                movementMaxYDifferential,
                movementMaxYDifferentialCrossLayer,
                movementMaxYDifferentialCrossPlatform,
                getAllActiveActorBlockingTilePositions);

            var actorController = actorGameObject.AddComponent<ActorController>();
            actorController.Init(actor, actionController, movementController);

            // Attach additional controller(s) to special actor
            #if PAL3
            switch ((PlayerActorId)actor.Id)
            {
                case PlayerActorId.HuaYing:
                    actorGameObject.AddComponent<HuaYingController>().Init(actor, actorController, actionController);
                    break;
                case PlayerActorId.LongKui:
                    actorGameObject.AddComponent<LongKuiController>().Init(actor, actionController);
                    break;
            }
            #elif PAL3A
            switch ((PlayerActorId)actor.Id)
            {
                case PlayerActorId.TaoZi:
                    actorGameObject.AddComponent<FlyingActorController>().Init(actor, actorController, actionController);
                    break;
            }
            #endif

            return actorGameObject;
        }
    }
}