// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Game.Actor.Controllers
{
    using Command;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Engine.Abstraction;

    public sealed class LongKuiController : GameEntityScript,
        ICommandExecutor<LongKuiSwitchModeCommand>
    {
        private ActorBase _actor;
        private ActorActionController _actionController;

        private int _currentMode = 0;

        protected override void OnEnableGameEntity()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        protected override void OnDisableGameEntity()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Init(ActorBase actor, ActorActionController actionController)
        {
            _actor = actor;
            _actionController = actionController;
        }

        public int GetCurrentMode()
        {
            return _currentMode;
        }

        public void Execute(LongKuiSwitchModeCommand command)
        {
            _currentMode = command.Mode;

            _actor.ChangeName(command.Mode == 0 ?
                ActorConstants.LongKuiBlueModeActorName :
                ActorConstants.LongKuiRedModeActorName);

            if (_actor.IsActive)
            {
                _actionController.PerformAction(_actor.GetIdleAction(), overwrite: true);
            }
        }
    }
}

#endif