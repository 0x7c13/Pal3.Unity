// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(149, "把背包里所获的的服装给龙葵，" +
                    "参数：服装ID变量")]
    public class LongKuiGetClothCommand : ICommand
    {
        public LongKuiGetClothCommand(ushort variable)
        {
            Variable = variable;
        }

        public ushort Variable { get; }
    }
}