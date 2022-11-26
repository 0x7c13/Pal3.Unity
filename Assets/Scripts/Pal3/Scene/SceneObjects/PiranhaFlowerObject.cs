// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.PiranhaFlower)]
    public class PiranhaFlowerObject : SceneObject
    {
        public PiranhaFlowerObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override bool IsInteractable(InteractionContext ctx)
        {
            return Activated && GameBoxInterpreter.IsPositionInsideRect(
                ObjectInfo.TileMapTriggerRect, ctx.ActorTilePosition);
        }

        public override void Interact(bool triggerredByPlayer)
        {
            #if PAL3
            // For PiranhaFlower object:
            // Parameters[0] = x1
            // Parameters[1] = y1
            // Parameters[2] = portal to flower object Id
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorSetTilePositionCommand(-1, ObjectInfo.Parameters[0], ObjectInfo.Parameters[1]));
            #elif PAL3A
            Vector3 worldPosition = GameBoxInterpreter.ToUnityPosition(new Vector3(ObjectInfo.Parameters[0], 0f, ObjectInfo.Parameters[1]));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorSetWorldPositionCommand(-1, worldPosition.x, worldPosition.z));
            #endif
        }
    }
}