// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(87, "播放门打开的动画")]
    public sealed class SceneOpenDoorCommand : ICommand
    {
        public SceneOpenDoorCommand(int objectId)
        {
            ObjectId = objectId;
        }

        public int ObjectId { get; }
    }
}