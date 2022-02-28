// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(146, "打开冶炼菜单")]
    public class UIShowSmeltingMenuCommand : ICommand
    {
        public UIShowSmeltingMenuCommand(int smeltingScriptId)
        {
            SmeltingScriptId = smeltingScriptId;
        }

        public int SmeltingScriptId { get; }
    }
}