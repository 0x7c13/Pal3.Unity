// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(78, "检查玩家是否拥有某个物品并与临时变量计算结果，" +
                    "参数：该物品的数据库ID")]
    public sealed class ScriptEvaluateVarIfPlayerHaveItemCommand : ICommand
    {
        public ScriptEvaluateVarIfPlayerHaveItemCommand(int itemId)
        {
            ItemId = itemId;
        }

        public int ItemId { get; }
    }
}