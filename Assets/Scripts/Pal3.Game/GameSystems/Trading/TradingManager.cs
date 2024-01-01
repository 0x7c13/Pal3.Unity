// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.GameSystems.Trading
{
    using System;
    using Command;
    using Core.Command;
    using Core.Command.SceCommands;

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
            Pal3.Instance.Execute(new UIDisplayNoteCommand("交易功能暂未开启"));
        }
    }
}