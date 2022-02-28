// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(78, "检查玩家是否拥有某个物品，" +
                    "参数：该物品的数据库ID，此命令结果影响标志变量（类似VarEqualTo）")]
    public class ScriptCheckIfPlayerHaveItemCommand : ICommand
    {
        public ScriptCheckIfPlayerHaveItemCommand(int itemId)
        {
            ItemId = itemId;
        }

        public int ItemId { get; }
    }
}