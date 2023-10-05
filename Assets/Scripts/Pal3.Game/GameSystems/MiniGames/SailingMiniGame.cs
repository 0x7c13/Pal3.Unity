// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Game.GameSystems.MiniGames
{
    using System;
    using Command;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;

    public sealed class SailingMiniGame : IDisposable,
        ICommandExecutor<MiniGameStartSailingCommand>
    {
        public SailingMiniGame()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Execute(MiniGameStartSailingCommand command)
        {
            if (command.StartSegment == 0)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new SceneLoadCommand("m04", "1"));
                CommandDispatcher<ICommand>.Instance.Dispatch(new ActorActivateCommand(ActorConstants.PlayerActorVirtualID, 0));
            }
            else if (command.StartSegment == 1)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new SceneLoadCommand("m05", "1"));
            }

            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunCommand(command.EndScriptId));

            CommandDispatcher<ICommand>.Instance.Dispatch(
                new UIDisplayNoteCommand("行船小游戏暂未实现，现已跳过"));
        }
    }
}

#endif