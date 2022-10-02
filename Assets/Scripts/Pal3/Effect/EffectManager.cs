// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Effect
{
    using Actor;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using MetaData;
    using Scene;
    using UnityEngine;

    public sealed class EffectManager : MonoBehaviour,
        ICommandExecutor<EffectAttachToActorCommand>,
        ICommandExecutor<EffectPlayCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private SceneManager _sceneManager;
        private int _targetActorId = -1;
        
        public void Init(SceneManager sceneManager)
        {
            _sceneManager = sceneManager;
        }
        
        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }
        
        public void Execute(EffectAttachToActorCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID) return;
            _targetActorId = command.ActorId;
        }
        
        public void Execute(EffectPlayCommand command)
        {
            if (_targetActorId == -1) return;
            
            #if PAL3A
            switch (command.EffectGroupId)
            {
                // 南宫煌形态切换为狼形态特效
                case 164:
                {
                    Scene scene = _sceneManager.GetCurrentScene();
                    Actor actor = scene.GetActor((byte)_targetActorId);
                    actor.ChangeName(ActorConstants.NanGongHuangWolfModeActorName);
                    if (actor.IsActive)
                    {
                        var actorActionController = scene.GetActorGameObject((byte)_targetActorId)
                            .GetComponent<ActorActionController>();
                        actorActionController.PerformAction(actor.GetIdleAction(), overwrite: true);
                    }
                    break;
                }
                // 南宫煌形态切换为人形态特效
                case 315:
                {
                    Scene scene = _sceneManager.GetCurrentScene();
                    Actor actor = scene.GetActor((byte)_targetActorId);
                    actor.ChangeName(ActorConstants.NanGongHuangHumanModeActorName);
                    if (actor.IsActive)
                    {
                        var actorActionController = scene.GetActorGameObject((byte)_targetActorId)
                            .GetComponent<ActorActionController>();
                        actorActionController.PerformAction(actor.GetIdleAction(), overwrite: true);
                    }
                    break;
                }
            }
            #endif

            _targetActorId = -1;
        }

        public void Execute(ResetGameStateCommand command)
        {
            _targetActorId = -1;
        }
    }
}