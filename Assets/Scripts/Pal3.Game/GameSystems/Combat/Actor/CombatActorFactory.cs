// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.GameSystems.Combat.Actor
{
    using System;
    using Combat;
    using Controllers;
    using Data;
    using Dev.Presenters;
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
    using Game.Actor;
    using Game.Actor.Controllers;

    using Color = Core.Primitives.Color;
    using Vector2Int = UnityEngine.Vector2Int;

    public static class CombatActorFactory
    {
        public static IGameEntity CreateCombatActorGameEntity(GameResourceProvider resourceProvider,
            CombatActor actor,
            ElementPosition elementPosition,
            bool isDropShadowEnabled)
        {
            IGameEntity actorGameEntity = GameEntityFactory.Create($"CombatActor_{actor.Id}_{actor.Name}");

            // Attach CombatActorInfo to the GameEntity for better debuggability
            #if UNITY_EDITOR
            CombatActorInfoPresenter combatActorInfoPresent = actorGameEntity.AddComponent<CombatActorInfoPresenter>();
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
                default:
                    throw new NotSupportedException($"Unsupported actor animation type: {actor.AnimationType}");
            }

            CombatActorController actorController = actorGameEntity.AddComponent<CombatActorController>();
            actorController.Init(actor, actionController, elementPosition);
            actorController.IsActive = true; // combat actor is always active

            return actorGameEntity;
        }
    }
}