// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(47, "拿走物品，" +
                    "参数：物品ID，个数")]
    public class InventoryRemoveItemCommand : ICommand
    {
        public InventoryRemoveItemCommand(int itemId)
        {
            ItemId = itemId;
        }

        public int ItemId { get; }
    }
}