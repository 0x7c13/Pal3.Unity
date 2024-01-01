// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3
    [SceCommand(123, "取出当前当铺经营游戏的当前UnHock类型并赋值给变量，" +
                    "参数：变量名")]
    public sealed class ScriptVarSetPawnshopUnHockTypeCommand : ICommand
    {
        public ScriptVarSetPawnshopUnHockTypeCommand(ushort variable)
        {
            Variable = variable;
        }

        [SceUserVariable] public ushort Variable { get; }
    }
    #endif
}