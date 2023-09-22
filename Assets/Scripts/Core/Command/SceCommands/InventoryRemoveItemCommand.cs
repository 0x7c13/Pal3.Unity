// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Command.SceCommands
{
    [SceCommand(47, "拿走一个物品，" +
                    "参数：物品ID")]
    public class InventoryRemoveItemCommand : ICommand
    {
        public InventoryRemoveItemCommand(int itemId)
        {
            ItemId = itemId;
        }

        public int ItemId { get; }
    }
}