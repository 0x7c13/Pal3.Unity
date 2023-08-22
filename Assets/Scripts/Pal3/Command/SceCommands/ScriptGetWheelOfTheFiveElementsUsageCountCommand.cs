// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3A
    [AvailableInConsole]
    [SceCommand(182, "取出五灵轮当前使用次数并赋值给变量，" +
                     "参数：变量名")]
    public class ScriptGetWheelOfTheFiveElementsUsageCountCommand : ICommand
    {
        public ScriptGetWheelOfTheFiveElementsUsageCountCommand(int variable)
        {
            Variable = variable;
        }

        public int Variable { get; }
    }
    #endif
}