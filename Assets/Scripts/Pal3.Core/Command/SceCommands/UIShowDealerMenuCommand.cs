// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(100, "调用买卖系统，" +
                     "参数：商店数据文件名")]
    public sealed class UIShowDealerMenuCommand : ICommand
    {
        public UIShowDealerMenuCommand(string dealerScriptName)
        {
            DealerScriptName = dealerScriptName;
        }

        public string DealerScriptName { get; }
    }
}