// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.MiniGame
{
    using System;
    using Command;
    using Command.SceCommands;
    using Core.Services;
    using MetaData;
    using Script;

    public sealed class HideFightMiniGame : IDisposable,
        ICommandExecutor<MiniGameStartHideFightCommand>
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
            if (ServiceLocator.Instance.Get<ScriptManager>().GetGlobalVariables()
                    [ScriptConstants.MainStoryVariableName] == 71000)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorSetTilePositionCommand(ActorConstants.PlayerActorVirtualID, 27, 113));
            }
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunCommand(1701));
        }
    }
}

#endif