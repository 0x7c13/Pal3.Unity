// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(48, "增减金钱，" +
                    "参数：增减量")]
    public class InventoryAddMoneyCommand : ICommand
    {
        public InventoryAddMoneyCommand(int changeAmount)
        {
            ChangeAmount = changeAmount;
        }

        public int ChangeAmount { get; }
    }
}