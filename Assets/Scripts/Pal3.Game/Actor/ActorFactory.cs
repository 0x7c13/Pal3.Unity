// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Actor
{
    using System;
    using System.Collections.Generic;
    using Controllers;
    using Core.Contract.Enums;
    using Data;
    using Dev.Presenters;
    using Engine.Abstraction;
    using GameSystems.Combat;
    using Scene;

    using Color = Core.Primitives.Color;
    using Vector2Int = UnityEngine.Vector2Int;

    public static class ActorFactory
    {
        public static IGameEntity CreateActorGameEntity(GameResourceProvider resourceProvider,
            Actor actor,
            Tilemap tilemap,
            bool isDropShadowEnabled,
            Color tintColor,
            float movementMaxYDifferential,
            float movementMaxYDifferentialCrossLayer,
            float movementMaxYDifferentialCrossPlatform,
            Func<int, int[], HashSet<Vector2Int>> getAllActiveActorBlockingTilePositions)
        {
            var actorGameEntity = new GameEntity($"Actor_{actor.Id}_{actor.Name}");

            // Attach ScnNpcInfo to the GameEntity for better debuggability
            #if UNITY_EDITOR
            var npcInfoPresent = actorGameEntity.AddComponent<NpcInfoPresenter>();
            npcInfoPresent.npcInfo = actor.Info;
            #endif

            bool hasColliderAndRigidBody = actor.HasColliderAndRigidBody();

            ActorActionController actionController;
            switch (actor.AnimationType)
            {
                case ActorAnimationType.Vertex:
                {
                    var autoStand = actor.Info.InitBehaviour != ActorBehaviourType.Hold;
                    var canPerformHoldAnimation = actor.Info is {InitBehaviour: ActorBehaviourType.Hold, LoopAction: 0};
                    VertexAnimationActorActionController vertexActionController =
                        actorGameEntity.AddComponent<VertexAnimationActorActionController>();
                    vertexActionController.Init(resourceProvider,
                        actor,
                        hasColliderAndRigidBody,
                        isDropShadowEnabled,
                        autoStand,
                        canPerformHoldAnimation,
                        tintColor);
                    actionController = vertexActionController;
                    break;
                }
                case ActorAnimationType.Skeletal:
                {
                    SkeletalAnimationActorActionController skeletalActionController =
                        actorGameEntity.AddComponent<SkeletalAnimationActorActionController>();
                    skeletalActionController.Init(resourceProvider,
                        actor,
                        hasColliderAndRigidBody,
                        isDropShadowEnabled,
                        tintColor);
                    actionController = skeletalActionController;
                    break;
                }
                default:
                    throw new NotSupportedException($"Unsupported actor animation type: {actor.AnimationType}");
            }

            var movementController = actorGameEntity.AddComponent<ActorMovementController>();
            movementController.Init(actor,
                tilemap,
                actionController,
                movementMaxYDifferential,
                movementMaxYDifferentialCrossLayer,
                movementMaxYDifferentialCrossPlatform,
                getAllActiveActorBlockingTilePositions);

            var actorController = actorGameEntity.AddComponent<ActorController>();
            actorController.Init(actor, actionController, movementController);

            // Attach additional controller(s) to special actor
            #if PAL3
            switch ((PlayerActorId)actor.Id)
            {
                case PlayerActorId.HuaYing:
                    actorGameEntity.AddComponent<HuaYingController>().Init(actor, actorController, actionController);
                    break;
                case PlayerActorId.LongKui:
                    actorGameEntity.AddComponent<LongKuiController>().Init(actor, actionController);
                    break;
            }
            #elif PAL3A
            switch ((PlayerActorId)actor.Id)
            {
                case PlayerActorId.TaoZi:
                    actorGameEntity.AddComponent<FlyingActorController>().Init(actor, actorController, actionController);
                    break;
            }
            #endif

            return actorGameEntity;
        }

        public static IGameEntity CreateCombatActorGameEntity(GameResourceProvider resourceProvider,
            CombatActor actor,
            ElementPosition elementPosition,
            bool isDropShadowEnabled)
        {
            var actorGameEntity = new GameEntity($"CombatActor_{actor.Id}_{actor.Name}");

            // Attach CombatActorInfo to the GameEntity for better debuggability
            #if UNITY_EDITOR
            var combatActorInfoPresent = actorGameEntity.AddComponent<CombatActorInfoPresenter>();
            combatActorInfoPresent.combatActorInfo = actor.Info;
            #endif

            ActorActionController actionController;
            switch (actor.AnimationType)
            {
                case ActorAnimationType.Vertex:
                {
                    VertexAnimationActorActionController vertexActionController =
                        actorGameEntity.AddComponent<VertexAnimationActorActionController>();
                    vertexActionController.Init(resourceProvider,
                        actor,
                        hasColliderAndRigidBody: false, // combat actor has no collider and rigidbody
                        isDropShadowEnabled,
                        autoStand: true, // combat actor always auto stand
                        canPerformHoldAnimation: false); // combat actor can't perform hold animation
                    actionController = vertexActionController;
                    break;
                }
                case ActorAnimationType.Skeletal:
                {
                    SkeletalAnimationActorActionController skeletalActionController =
                        actorGameEntity.AddComponent<SkeletalAnimationActorActionController>();
                    skeletalActionController.Init(resourceProvider,
                        actor,
                        hasColliderAndRigidBody: false, // combat actor has no collider and rigidbody
                        isDropShadowEnabled);
                    actionController = skeletalActionController;
                    break;
                }
                default:
                    throw new NotSupportedException($"Unsupported actor animation type: {actor.AnimationType}");
            }

            var actorController = actorGameEntity.AddComponent<CombatActorController>();
            actorController.Init(actor, actionController, elementPosition);
            actorController.IsActive = true; // combat actor is always active

            return actorGameEntity;
        }
    }
}