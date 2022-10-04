// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Actor
{
    using Command;
    using Command.SceCommands;
    using MetaData;
    using UnityEngine;

    public sealed class LongKuiController : MonoBehaviour,
        ICommandExecutor<LongKuiSwitchModeCommand>
    {
        private Actor _actor;
        private ActorActionController _actionController;

        private int _currentMode = 0;

        public void Init(Actor actor, ActorActionController actionController)
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
                ActorConstants.LongKuiHumanModeActorName :
                ActorConstants.LongKuiGhostModeActorName);
            
            if (_actor.IsActive)
            {
                _actionController.PerformAction(_actor.GetIdleAction(), overwrite: true);   
            }
        }
    }
}

#endif