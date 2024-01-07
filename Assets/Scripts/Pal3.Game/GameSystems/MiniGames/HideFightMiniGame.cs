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
    using Core.Utilities;
    using Script;

    public sealed class HideFightMiniGame : IDisposable,
        ICommandExecutor<MiniGameStartHideFightCommand>
    {
        private readonly IUserVariableStore<ushort, int> _userVariableStore;

        public HideFightMiniGame(IUserVariableStore<ushort, int> userVariableStore)
        {
            _userVariableStore = Requires.IsNotNull(userVariableStore, nameof(userVariableStore));
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Execute(MiniGameStartHideFightCommand command)
        {
            if (_userVariableStore.Get(ScriptConstants.MainStoryVariableId) == 71000)
            {
                Pal3.Instance.Execute(new ActorSetTilePositionCommand(ActorConstants.PlayerActorVirtualID, 27, 113));
            }
            Pal3.Instance.Execute(new ScriptExecuteCommand(1701));

            Pal3.Instance.Execute(new UIDisplayNoteCommand("此处小游戏暂未实现，现已跳过"));
        }
    }
}

#endif