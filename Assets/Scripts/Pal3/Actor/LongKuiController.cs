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

    public class LongKuiController : MonoBehaviour,
        ICommandExecutor<LongKuiSwitchModeCommand>
    {
        private Actor _actor;
        private ActorActionController _actionController;

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
        
        public void Execute(LongKuiSwitchModeCommand command)
        {
            _actor.ChangeName(command.Mode == 0 ?
                ActorConstants.LongKuiHumanModeActorName :
                ActorConstants.LongKuiGhostModeActorName);
            _actionController.PerformAction(_actor.GetIdleAction(), overwrite: true);
        }
    }
}

#endif