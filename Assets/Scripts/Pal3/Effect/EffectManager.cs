// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Effect
{
    using System;
    using Actor;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.GameBox;
    using Data;
    using MetaData;
    using Scene;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public sealed class EffectManager : MonoBehaviour, IDisposable,
        ICommandExecutor<EffectPreLoadCommand>,
        ICommandExecutor<EffectAttachToActorCommand>,
        ICommandExecutor<EffectSetPositionCommand>,
        ICommandExecutor<EffectPlayCommand>,
        ICommandExecutor<SceneLeavingCurrentSceneNotification>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private GameResourceProvider _resourceProvider;
        private SceneManager _sceneManager;
        private ICommand _effectPositionCommand;

        public void Init(GameResourceProvider resourceProvider,
            SceneManager sceneManager)
        {
            _resourceProvider = resourceProvider ?? throw new ArgumentNullException(nameof(resourceProvider));
            _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
        }
        
        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Dispose()
        {
            _effectPositionCommand = null;
        }
        
        public void Execute(EffectPreLoadCommand command)
        {
            // In PAL3A, PreLoad command is always issued right before the effect is played.
            // So it is pointless to pre-load the effect here for PAL3A.
            #if PAL3
            StartCoroutine(_resourceProvider.PreLoadVfxEffectAsync(command.EffectGroupId));
            #endif
        }
        
        public void Execute(EffectSetPositionCommand command)
        {
            _effectPositionCommand = command;
        }
        
        public void Execute(EffectAttachToActorCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID) return;
            _effectPositionCommand = command;
        }

        public void Execute(EffectPlayCommand command)
        {
            if (_effectPositionCommand == null ||
                _sceneManager.GetCurrentScene() is not {} currentScene) return;
            
            #if PAL3A
            if (_effectPositionCommand is EffectAttachToActorCommand effectAttachToActorCommand)
            {
                HandleNanGongHuangTransformEffect(effectAttachToActorCommand, command, currentScene);
            }
            #endif

            // Play VFX
            Object vfxPrefab = _resourceProvider.GetVfxEffectPrefab(command.EffectGroupId);
            if (vfxPrefab != null)
            {
                Transform parent = null;
                Vector3 localPosition = Vector3.zero;
                
                if (_effectPositionCommand is EffectSetPositionCommand positionCommand &&
                    _sceneManager.GetSceneRootGameObject() is {} sceneRootGameObject)
                {
                    parent = sceneRootGameObject.transform;
                    localPosition = GameBoxInterpreter.ToUnityPosition(
                        new Vector3(positionCommand.X, positionCommand.Y, positionCommand.Z));
                }
                else if (_effectPositionCommand is EffectAttachToActorCommand actorCommand)
                {
                    parent = currentScene.GetActorGameObject((byte)actorCommand.ActorId).transform;
                }

                if (parent != null)
                {
                    var vfx = (GameObject)Instantiate(vfxPrefab, parent, false);
                    vfx.name = "VFX_" + command.EffectGroupId;
                    vfx.transform.localPosition += localPosition; 
                }
            }

            // Play SFX if any
            if (EffectConstants.EffectSfxInfo.ContainsKey(command.EffectGroupId))
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new PlaySfxCommand(EffectConstants.EffectSfxInfo[command.EffectGroupId], 1));
            }
        }

        #if PAL3A
        private void HandleNanGongHuangTransformEffect(EffectAttachToActorCommand effectAttachToActorCommand,
            EffectPlayCommand effectPlayCommand,
            Scene currentScene)
        {
            var actorId = effectAttachToActorCommand.ActorId;
            switch (effectPlayCommand.EffectGroupId)
            {
                // 南宫煌形态切换为狼形态特效
                case 164:
                {
                    Actor actor = currentScene.GetActor((byte)actorId);
                    actor.ChangeName(ActorConstants.NanGongHuangWolfModeActorName);
                    if (actor.IsActive)
                    {
                        var actorActionController = currentScene.GetActorGameObject((byte)actorId)
                            .GetComponent<ActorActionController>();
                        actorActionController.PerformAction(actor.GetIdleAction(), overwrite: true);
                    }
                    break;
                }
                // 南宫煌形态切换为人形态特效
                case 315:
                {
                    Actor actor = currentScene.GetActor((byte)actorId);
                    actor.ChangeName(ActorConstants.NanGongHuangHumanModeActorName);
                    if (actor.IsActive)
                    {
                        var actorActionController = currentScene.GetActorGameObject((byte)actorId)
                            .GetComponent<ActorActionController>();
                        actorActionController.PerformAction(actor.GetIdleAction(), overwrite: true);
                    }
                    break;
                }
            }
        }
        #endif
        
        public void Execute(SceneLeavingCurrentSceneNotification command)
        {
            Dispose();
        }
        
        public void Execute(ResetGameStateCommand command)
        {
            Dispose();
        }
    }
}