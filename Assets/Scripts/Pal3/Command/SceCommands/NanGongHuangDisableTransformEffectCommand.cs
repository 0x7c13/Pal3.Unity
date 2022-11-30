// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3A
    [AvailableInConsole]
    [SceCommand(200, "关闭南宫煌身体异常时（快要变身）的特效")]
    public class NanGongHuangDisableTransformEffectCommand : ICommand
    {
        public NanGongHuangDisableTransformEffectCommand() { }
    }
    #endif
}