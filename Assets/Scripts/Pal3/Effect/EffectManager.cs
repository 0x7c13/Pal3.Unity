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
    using MetaData;
    using Scene;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public sealed class EffectManager : MonoBehaviour,
        ICommandExecutor<EffectAttachToActorCommand>,
        ICommandExecutor<EffectSetPositionCommand>,
        ICommandExecutor<EffectPlayCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private SceneManager _sceneManager;

        private ICommand _effectPositionCommand;
        
        public void Init(SceneManager sceneManager)
        {
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
            
            var parent = new GameObject($"VFX_{command.EffectGroupId}");
            parent.transform.localScale = Vector3.one;

            if (_effectPositionCommand is EffectSetPositionCommand positionCommand &&
                _sceneManager.GetSceneRootGameObject() is {} sceneRootGameObject)
            {
                parent.transform.SetParent(sceneRootGameObject.transform);
                parent.transform.position = GameBoxInterpreter.ToUnityPosition(
                    new Vector3(positionCommand.X, positionCommand.Y, positionCommand.Z));
            }

            if (_effectPositionCommand is EffectAttachToActorCommand actorCommand)
            {
                Transform actorTransform = currentScene.GetActorGameObject((byte)actorCommand.ActorId).transform;
                parent.transform.SetParent(actorTransform);
                parent.transform.localPosition = Vector3.zero;
            }

            Object vfxPrefab = Resources.Load($"Prefabs/VFX/{GameConstants.AppName}/{command.EffectGroupId}");
            if (vfxPrefab != null)
            {
                Instantiate(vfxPrefab, parent.transform, false);
            }
            else
            {
                Debug.LogWarning("VFX prefab not found: " + command.EffectGroupId);
            }

            //_effectPositionCommand = null;
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

        public void Execute(ResetGameStateCommand command)
        {
            _effectPositionCommand = null;
        }
    }
}