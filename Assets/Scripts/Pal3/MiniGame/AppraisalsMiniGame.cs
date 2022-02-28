// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.MiniGame
{
    using Command;
    using Command.SceCommands;

    public class AppraisalsMiniGame : ICommandExecutor<MiniGameStartAppraisalsCommand>
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
        }
    }
}