// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

#if PAL3A
namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(198, "解除相机水平朝向约束")]
    public sealed class CameraResetHorizontalDirectionCommand : ICommand
    {
        public CameraResetHorizontalDirectionCommand()
        {
        }
    }
}
#endif
