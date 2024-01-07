﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(132, "取得战斗结果（0输1赢）并赋值给变量，" +
                     "参数：变量名")]
    public sealed class ScriptVarSetCombatResultCommand : ICommand
    {
        public ScriptVarSetCombatResultCommand(ushort variable)
        {
            Variable = variable;
        }

        [SceUserVariable] public ushort Variable { get; }
    }
}