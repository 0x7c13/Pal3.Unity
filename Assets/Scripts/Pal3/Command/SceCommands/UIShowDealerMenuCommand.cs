// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
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