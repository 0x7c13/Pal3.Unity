// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.MiniGame
{
    using Command;
    using Command.SceCommands;

    public class EncampMiniGame : ICommandExecutor<MiniGameStartEncampCommand>
    {
        public EncampMiniGame()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Execute(MiniGameStartEncampCommand command)
        {
        }
    }
}

#endif