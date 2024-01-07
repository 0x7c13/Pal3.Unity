// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(250, "镜头是否锁定并跟随在主角身上，" +
                     "参数：1锁定并跟随，0解锁")]
    public sealed class CameraFollowPlayerCommand : ICommand
    {
        public CameraFollowPlayerCommand(int follow)
        {
            Follow = follow;
        }

        public int Follow { get; }
    }
}