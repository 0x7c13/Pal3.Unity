// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Command.SceCommands
{
    [SceCommand(39, "取出镜头状态")]
    public class CameraPopStateCommand : ICommand
    {
        public CameraPopStateCommand() {}
    }
}