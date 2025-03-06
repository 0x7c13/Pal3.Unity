// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(47, "拿走一个物品，" +
                    "参数：物品ID")]
    public sealed class InventoryRemoveItemCommand : ICommand
    {
        public InventoryRemoveItemCommand(int itemId)
        {
            ItemId = itemId;
        }

        public int ItemId { get; }
    }
}