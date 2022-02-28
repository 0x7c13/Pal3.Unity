// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.MiniGame
{
    using Command;
    using Command.SceCommands;

    public class HideFightMiniGame : ICommandExecutor<MiniGameStartHideFightCommand>
    {
        public HideFightMiniGame()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Execute(MiniGameStartHideFightCommand command)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunCommand(1701));
        }
    }
}