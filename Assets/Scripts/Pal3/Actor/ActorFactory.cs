// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Actor
{
    using Data;
    using MetaData;
    using Scene;
    using UnityEngine;

    public static class ActorFactory
    {
        public static GameObject CreateActorGameObject(
            GameResourceProvider resourceProvider,
            Actor actor,
            GameObject parent,
            Tilemap tilemap,
            Color tintColor)
        {
            var actorGameObject = new GameObject($"Actor_{actor.Info.Id}_{actor.Info.Name}");

            // Attach ScnNpcInfo to the GameObject for better debuggability
            #if UNITY_EDITOR
            var npcInfoPresent = actorGameObject.AddComponent<NpcInfoPresenter>();
            npcInfoPresent.NpcInfo = actor.Info;
            #endif

            var actionController = actorGameObject.AddComponent<ActorActionController>();
            actionController.Init(actor, tintColor);

            var movementController = actorGameObject.AddComponent<ActorMovementController>();
            movementController.Init(actor, tilemap, actionController);

            var actorController = actorGameObject.AddComponent<ActorController>();
            actorController.Init(resourceProvider, actor, actionController, movementController);

            #if PAL3
            if (actor.Info.Id == (byte)PlayerActorId.HuaYing)
            {
                actorGameObject.AddComponent<HuaYingController>().Init(actorController, actionController);
            }
            #endif

            actorGameObject.transform.SetParent(parent.transform, false);

            return actorGameObject;
        }
    }
}