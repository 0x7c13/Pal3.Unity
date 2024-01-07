// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3A
    [SceCommand(199, "开启南宫煌身体异常时（快要变身）的特效")]
    public sealed class NanGongHuangEnableTransformEffectCommand : ICommand
    {
        public NanGongHuangEnableTransformEffectCommand() { }
    }
    #endif
}