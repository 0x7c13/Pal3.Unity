// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(38, "保存镜头状态")]
    public sealed class CameraPushStateCommand : ICommand
    {
        public CameraPushStateCommand() {}
    }
}