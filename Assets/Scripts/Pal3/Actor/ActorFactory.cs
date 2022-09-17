// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
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
            Color tintColor,
            Func<int, byte[], HashSet<Vector2Int>> getAllActiveActorBlockingTilePositions)
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

            var actionController = actorGameObject.AddComponent<ActorActionController>();
            actionController.Init(resourceProvider, actor, tintColor);

            var movementController = actorGameObject.AddComponent<ActorMovementController>();
            movementController.Init(actor, tilemap, actionController, getAllActiveActorBlockingTilePositions);

            var actorController = actorGameObject.AddComponent<ActorController>();
            actorController.Init(resourceProvider, actor, actionController, movementController);

            #if PAL3
            if (actor.Info.Id == (byte)PlayerActorId.HuaYing)
            {
                actorGameObject.AddComponent<HuaYingController>().Init(actorController, actionController);
            }
            #elif PAL3A
            if (actor.Info.Id == (byte) PlayerActorId.TaoZi)
            {
                actorGameObject.AddComponent<FlyingActorController>().Init(actionController);
            }
            #endif

            return actorGameObject;
        }
    }
}