// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3A
    [SceCommand(182, "取出五灵轮当前使用次数并赋值给变量，" +
                     "参数：变量名")]
    public sealed class ScriptVarSetWheelOfTheFiveElementsUsageCountCommand : ICommand
    {
        public ScriptVarSetWheelOfTheFiveElementsUsageCountCommand(ushort variable)
        {
            Variable = variable;
        }

        [SceUserVariable] public ushort Variable { get; }
    }
    #endif
}