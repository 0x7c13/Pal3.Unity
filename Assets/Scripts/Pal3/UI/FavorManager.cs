// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.UI
{
    using System.Collections.Generic;
    using System.Linq;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;

    public class FavorManager :
        ICommandExecutor<FavorAddCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private const int DEFAULT_FAVOR_AMOUNT = 20;

        private readonly Dictionary<int, int> _actorFavor = new ();

        public FavorManager()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public int GetCalculatedFavor(int actorId)
        {
            if (_actorFavor.ContainsKey(actorId))
            {
                return DEFAULT_FAVOR_AMOUNT + _actorFavor[actorId];
            }

            return DEFAULT_FAVOR_AMOUNT;
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