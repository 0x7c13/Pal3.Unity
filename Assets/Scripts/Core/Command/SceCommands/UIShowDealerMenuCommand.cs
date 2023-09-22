// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Command.SceCommands
{
    [SceCommand(100, "调用买卖系统，" +
                     "参数：商店数据文件名")]
    public class UIShowDealerMenuCommand : ICommand
    {
        public UIShowDealerMenuCommand(string dealerScriptName)
        {
            DealerScriptName = dealerScriptName;
        }

        public string DealerScriptName { get; }
    }
}