// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
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
                Pal3.Instance.Execute(new SceneLoadCommand("m04", "1"));
                Pal3.Instance.Execute(new ActorActivateCommand(ActorConstants.PlayerActorVirtualID, 0));
            }
            else if (command.StartSegment == 1)
            {
                Pal3.Instance.Execute(new SceneLoadCommand("m05", "1"));
            }

            Pal3.Instance.Execute(new ScriptExecuteCommand(command.EndScriptId));

            Pal3.Instance.Execute(new UIDisplayNoteCommand("行船小游戏暂未实现，现已跳过"));
        }
    }
}

#endif