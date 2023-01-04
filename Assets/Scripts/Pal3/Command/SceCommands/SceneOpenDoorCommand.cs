// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(87, "播放门打开的动画")]
    public class SceneOpenDoorCommand : ICommand
    {
        public SceneOpenDoorCommand(int objectId)
        {
            ObjectId = objectId;
        }

        public int ObjectId { get; }
    }
}