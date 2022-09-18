// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.MiniGame
{
    using Command;
    using Command.SceCommands;

    public class SwatAFlyMiniGame : ICommandExecutor<MiniGameStartSwatAFlyCommand>
    {
        public SwatAFlyMiniGame()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Execute(MiniGameStartSwatAFlyCommand command)
        {
        }
    }
}

#endif