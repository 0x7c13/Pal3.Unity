// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.GameSystems.Favor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Enums;
    using Engine.Logging;

    public sealed class FavorManager : IDisposable,
        ICommandExecutor<FavorAddCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private const int BASE_FAVOR_AMOUNT = 20;

        private readonly Dictionary<int, int> _actorFavor = new ();

        public FavorManager()
        {
            Init();
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        private void Init()
        {
            // Only female characters are supported (by OG game design)
            #if PAL3
            _actorFavor[(int)PlayerActorId.XueJian] = 0;
            _actorFavor[(int)PlayerActorId.LongKui] = 0;
            _actorFavor[(int)PlayerActorId.ZiXuan] = 0;
            _actorFavor[(int)PlayerActorId.HuaYing] = 0;
            #elif PAL3A
            _actorFavor[(int)PlayerActorId.WenHui] = 0;
            _actorFavor[(int)PlayerActorId.WangPengXu] = 0;
            #endif
        }

        public Dictionary<int, int> GetAllActorFavorInfo()
        {
            return _actorFavor;
        }

        public int GetFavorByActor(int actorId)
        {
            return _actorFavor.TryGetValue(actorId, out var favor) ?
                favor + BASE_FAVOR_AMOUNT :
                BASE_FAVOR_AMOUNT;
        }

        public void Execute(FavorAddCommand command)
        {
            if (_actorFavor.ContainsKey(command.ActorId))
            {
                _actorFavor[command.ActorId] += command.ChangeAmount;
            }
            else
            {
                EngineLogger.LogError($"Actor ID {command.ActorId} is not supported for favor system. " +
                               $"Valid IDs are: {string.Join(", ", _actorFavor.Keys)}");
            }
        }

        public void Execute(ResetGameStateCommand command)
        {
            _actorFavor.Clear();
            Init(); // Re-init the favor values.
        }

        public int GetMostFavorableActorId()
        {
            return _actorFavor.First(
                favor => favor.Value == _actorFavor.Max(f => f.Value)).Key;
        }

        public int GetLeastFavorableActorId()
        {
            return _actorFavor.First(
                favor => favor.Value == _actorFavor.Min(f => f.Value)).Key;
        }
    }
}