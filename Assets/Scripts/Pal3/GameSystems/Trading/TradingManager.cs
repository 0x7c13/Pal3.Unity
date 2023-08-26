// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.GameSystems.Trading
{
    using System;
    using Command;
    using Command.SceCommands;

    public sealed class TradingManager : IDisposable,
        ICommandExecutor<UIShowDealerMenuCommand>
    {
        public TradingManager()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        // TODO: Remove this
        public void Execute(UIShowDealerMenuCommand command)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand("交易功能暂未开启"));
        }
    }
}