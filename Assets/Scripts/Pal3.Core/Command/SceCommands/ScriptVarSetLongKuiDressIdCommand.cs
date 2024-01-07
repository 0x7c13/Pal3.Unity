// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3
    [SceCommand(149, "把背包里所获的的服装给龙葵，" +
                    "参数：服装ID变量")]
    public sealed class ScriptVarSetLongKuiDressIdCommand : ICommand
    {
        public ScriptVarSetLongKuiDressIdCommand(ushort variable)
        {
            Variable = variable;
        }

        [SceUserVariable] public ushort Variable { get; }
    }
    #endif
}