// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3
    [SceCommand(108, "取出鉴定小游戏的结果并设置给变量" +
                     "参数：用户变量，0失败，1成功")]
    public sealed class ScriptVarSetAppraisalsResultCommand : ICommand
    {
        public ScriptVarSetAppraisalsResultCommand(ushort variable)
        {
            Variable = variable;
        }

        [SceUserVariable] public ushort Variable { get; }
    }
    #endif
}