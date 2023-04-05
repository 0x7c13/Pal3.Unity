// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Actor
{
    using System;
    using System.Collections.Generic;
    using Data;
    using Dev;
    using MetaData;
    using Scene;
    using UnityEngine;

    public static class ActorFactory
    {
        public static GameObject CreateActorGameObject(GameResourceProvider resourceProvider,
            Actor actor,
            Tilemap tilemap,
            bool isDropShadowEnabled,
            Color tintColor,
            Func<int, int[], HashSet<Vector2Int>> getAllActiveActorBlockingTilePositions)
        {
            var actorGameObject = new GameObject($"Actor_{actor.Info.Id}_{actor.Info.Name}")
            {
                layer = LayerMask.NameToLayer("Ignore Raycast")
            };

            // Attach ScnNpcInfo to the GameObject for better debuggability
            #if UNITY_EDITOR
            var npcInfoPresent = actorGameObject.AddComponent<NpcInfoPresenter>();
            npcInfoPresent.npcInfo = actor.Info;
            #endif

            #if PAL3
            var hasColliderAndRigidBody = (PlayerActorId) actor.Info.Id != PlayerActorId.HuaYing;
            #elif PAL3A
            var hasColliderAndRigidBody = (PlayerActorId) actor.Info.Id != PlayerActorId.TaoZi;
            #endif

            ActorActionController actionController;
            if (actor.AnimationFileType == ActorAnimationFileType.Mv3)
            {
                Mv3ActorActionController mv3ActionController = actorGameObject.AddComponent<Mv3ActorActionController>();
                mv3ActionController.Init(resourceProvider, actor, hasColliderAndRigidBody, isDropShadowEnabled, tintColor);
                actionController = mv3ActionController;
            }
            else
            {
                MovActorActionController movActionController = actorGameObject.AddComponent<MovActorActionController>();
                movActionController.Init(resourceProvider, actor, hasColliderAndRigidBody, isDropShadowEnabled, tintColor);
                actionController = movActionController;
            }

            var movementController = actorGameObject.AddComponent<ActorMovementController>();
            movementController.Init(actor, tilemap, actionController, getAllActiveActorBlockingTilePositions);

            var actorController = actorGameObject.AddComponent<ActorController>();
            actorController.Init(actor, actionController, movementController);

            // Attach additional controller(s) to special actor
            #if PAL3
            switch ((PlayerActorId)actor.Info.Id)
            {
                case PlayerActorId.HuaYing:
                    actorGameObject.AddComponent<HuaYingController>().Init(actor, actorController, actionController);
                    break;
                case PlayerActorId.LongKui:
                    actorGameObject.AddComponent<LongKuiController>().Init(actor, actionController);
                    break;
            }
            #elif PAL3A
            switch ((PlayerActorId)actor.Info.Id)
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