// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
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
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
    using Scene;

    using Color = Core.Primitives.Color;
    using Vector2Int = UnityEngine.Vector2Int;

    public static class ActorFactory
    {
        public static IGameEntity CreateActorGameEntity(GameResourceProvider resourceProvider,
            GameActor actor,
            Tilemap tilemap,
            bool isDropShadowEnabled,
            Color tintColor,
            float movementMaxYDifferential,
            float movementMaxYDifferentialCrossLayer,
            float movementMaxYDifferentialCrossPlatform,
            Func<int, int[], HashSet<Vector2Int>> getAllActiveActorBlockingTilePositions)
        {
            IGameEntity actorGameEntity = GameEntityFactory.Create($"Actor_{actor.Id}_{actor.Name}");

            // Attach ScnNpcInfo to the GameEntity for better debuggability
            #if UNITY_EDITOR
            NpcInfoPresenter npcInfoPresent = actorGameEntity.AddComponent<NpcInfoPresenter>();
            npcInfoPresent.npcInfo = actor.Info;
            #endif

            bool hasColliderAndRigidBody = actor.HasColliderAndRigidBody();

            ActorActionController actionController;
            switch (actor.AnimationType)
            {
                case ActorAnimationType.Vertex:
                {
                    bool autoStand = actor.Info.InitBehaviour != ActorBehaviourType.Hold;
                    bool canPerformHoldAnimation = actor.Info is {InitBehaviour: ActorBehaviourType.Hold, LoopAction: 0};
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

            ActorMovementController movementController = actorGameEntity.AddComponent<ActorMovementController>();
            movementController.Init(actor,
                tilemap,
                actionController,
                movementMaxYDifferential,
                movementMaxYDifferentialCrossLayer,
                movementMaxYDifferentialCrossPlatform,
                getAllActiveActorBlockingTilePositions);

            ActorController actorController = actorGameEntity.AddComponent<ActorController>();
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
    }
}