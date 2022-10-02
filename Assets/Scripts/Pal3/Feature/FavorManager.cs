// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Feature
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using MetaData;

    public class FavorManager :
        ICommandExecutor<FavorAddCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private const int DEFAULT_FAVOR_AMOUNT = 20;

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
            foreach (var actorId in Enum.GetValues(typeof(PlayerActorId)).Cast<int>())
            {
                if (actorId == 0) continue; // Skip for the main actor
                _actorFavor[actorId] = DEFAULT_FAVOR_AMOUNT;
            }
        }
        
        public int GetFavorByActor(int actorId)
        {
            return _actorFavor.ContainsKey(actorId) ?
                _actorFavor[actorId] : DEFAULT_FAVOR_AMOUNT;
        }

        public void Execute(FavorAddCommand command)
        {
            if (_actorFavor.ContainsKey(command.ActorId))
            {
                _actorFavor[command.ActorId] += command.ChangeAmount;
            }
            else
            {
                _actorFavor[command.ActorId] = command.ChangeAmount;
            }
        }

        public void Execute(ResetGameStateCommand command)
        {
            _actorFavor.Clear();
            Init();
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