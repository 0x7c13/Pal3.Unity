// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Command;
    using Command.SceCommands;
    using Core.DataReader.Scn;

    [ScnSceneObject(ScnSceneObjectType.PiranhaFlower)]
    public class PiranhaFlowerObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 6f;

        public PiranhaFlowerObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override bool IsInteractable(float distance)
        {
            return distance < MAX_INTERACTION_DISTANCE;
        }

        public override void Interact()
        {
            // For PiranhaFlower object:
            // Parameters[0] = x1
            // Parameters[1] = y1
            // Parameters[2] = portal to flower object Id

            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorSetTilePositionCommand(-1, Info.Parameters[0], Info.Parameters[1]));
        }
    }
}