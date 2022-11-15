// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3
    [AvailableInConsole]
    [SceCommand(159, "初始化神树之花")]
    public class SceneInitDivineTreeFlowerCommand : ICommand
    {
        public SceneInitDivineTreeFlowerCommand() {}
    }
    #endif
}