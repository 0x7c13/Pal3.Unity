// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3A

namespace Pal3.Actor
{
    using Command;
    using Command.SceCommands;
    using MetaData;
    using UnityEngine;

    public class NanGongHuangController : MonoBehaviour,
        ICommandExecutor<EffectPlayCommand>
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

        public void Execute(EffectPlayCommand command)
        {
            switch (command.EffectGroupId)
            {
                // 南宫煌形态切换为狼形态特效
                case 164:
                {
                    _actor.ChangeName(ActorConstants.NanGongHuangWolfModeActorName);
                    if (_actor.IsActive) _actionController.PerformAction(_actor.GetIdleAction(), overwrite: true);
                    break;
                }
                // 南宫煌形态切换为人形态特效
                case 315:
                {
                    _actor.ChangeName(ActorConstants.NanGongHuangHumanModeActorName);
                    if (_actor.IsActive) _actionController.PerformAction(_actor.GetIdleAction(), overwrite: true);
                    break;
                }
            }
        }
    }
}

#endif