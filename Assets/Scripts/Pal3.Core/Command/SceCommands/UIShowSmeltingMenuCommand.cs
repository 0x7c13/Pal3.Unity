// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(146, "打开冶炼菜单")]
    public sealed class UIShowSmeltingMenuCommand : ICommand
    {
        public UIShowSmeltingMenuCommand(int smeltingScriptId)
        {
            SmeltingScriptId = smeltingScriptId;
        }

        public int SmeltingScriptId { get; }
    }
}