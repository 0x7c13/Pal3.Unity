// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.MiniGame
{
    using Command;
    using Command.SceCommands;

    public class SailingMiniGame : ICommandExecutor<MiniGameStartSailingCommand>
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
            }
            else if (command.StartSegment == 1)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new SceneLoadCommand("m05", "1"));
            }

            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunCommand(command.EndScriptId));
        }
    }
}