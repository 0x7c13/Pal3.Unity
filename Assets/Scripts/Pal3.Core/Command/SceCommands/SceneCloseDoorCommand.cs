// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3A
    [SceCommand(187, "播放门关闭的动画")]
    public sealed class SceneCloseDoorCommand : ICommand
    {
        public SceneCloseDoorCommand(int objectId)
        {
            ObjectId = objectId;
        }

        public int ObjectId { get; }
    }
    #endif
}