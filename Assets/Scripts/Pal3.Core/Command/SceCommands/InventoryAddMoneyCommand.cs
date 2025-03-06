// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(48, "增减金钱，" +
                    "参数：增减量")]
    public sealed class InventoryAddMoneyCommand : ICommand
    {
        public InventoryAddMoneyCommand(int changeAmount)
        {
            ChangeAmount = changeAmount;
        }

        public int ChangeAmount { get; }
    }
}