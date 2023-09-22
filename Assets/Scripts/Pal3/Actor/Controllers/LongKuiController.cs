// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Actor.Controllers
{
    using Command;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using UnityEngine;

    public sealed class LongKuiController : MonoBehaviour,
        ICommandExecutor<LongKuiSwitchModeCommand>
    {
        private ActorBase _actor;
        private ActorActionController _actionController;

        private int _currentMode = 0;

        public void Init(ActorBase actor, ActorActionController actionController)
        {
            _actor = actor;
            _actionController = actionController;
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
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