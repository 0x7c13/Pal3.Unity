// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.GameSystems.MiniGames
{
    using System;
    using Command;
    using Core.Command;
    using Core.Command.SceCommands;

    public sealed class AppraisalsMiniGame : IDisposable,
        ICommandExecutor<MiniGameStartAppraisalsCommand>
    {
        public AppraisalsMiniGame()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public bool GetResult()
        {
            return true;
        }

        public void Execute(MiniGameStartAppraisalsCommand command)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new UIDisplayNoteCommand("鉴宝小游戏暂未实现，现已跳过"));
        }
    }
}

#endif