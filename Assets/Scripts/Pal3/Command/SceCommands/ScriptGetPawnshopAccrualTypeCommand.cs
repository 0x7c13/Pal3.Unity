// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(122, "取出当前当铺经营游戏的当前Accrual类型并赋值给变量，" +
                    "参数：变量名")]
    public class ScriptGetPawnshopAccrualTypeCommand : ICommand
    {
        public ScriptGetPawnshopAccrualTypeCommand(int variable)
        {
            Variable = variable;
        }

        public int Variable { get; }
    }
}