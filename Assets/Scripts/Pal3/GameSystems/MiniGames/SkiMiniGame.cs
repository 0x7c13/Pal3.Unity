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
    using Core.Utilities;
    using Script;

    public sealed class SkiMiniGame : IDisposable,
        ICommandExecutor<MiniGameStartSkiCommand>
    {
        private readonly ScriptManager _scriptManager;

        public SkiMiniGame(ScriptManager scriptManager)
        {
            _scriptManager = Requires.IsNotNull(scriptManager, nameof(scriptManager));
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Execute(MiniGameStartSkiCommand command)
        {
            _scriptManager.AddScript((uint)command.EndGameScriptId);

            CommandDispatcher<ICommand>.Instance.Dispatch(
                new UIDisplayNoteCommand("滑雪小游戏暂未实现，现已跳过"));
        }
    }
}

#endif