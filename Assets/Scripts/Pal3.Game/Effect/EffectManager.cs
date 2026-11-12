// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect
{
    using System;
    using Actor;
    using Actor.Controllers;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.DataReader.Dat;
    using Core.Primitives;
    using Core.Utilities;
    using Data;
    using Engine.Extensions;
    using Scene;

    using Vector3 = UnityEngine.Vector3;

    public sealed class EffectManager : IDisposable,
        ICommandExecutor<EffectPreLoadCommand>,
        ICommandExecutor<EffectAttachToActorCommand>,
        ICommandExecutor<EffectSetPositionCommand>,
        ICommandExecutor<EffectPlayCommand>,
        ICommandExecutor<SceneLeavingCurrentSceneNotification>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private readonly OptionalVfxPlayer _optionalVfx;
        private readonly SceneManager _sceneManager;
        private ICommand _effectPositionCommand;

        private const string EFFECT_LINKER_FILE_NAME = "efflinker.dat";
        private EffectLinkerFile _effectLinkerFile;

        public EffectManager(GameResourceProvider resourceProvider,
            SceneManager sceneManager)
        {
            _optionalVfx = Requires.IsNotNull(resourceProvider, nameof(resourceProvider)).OptionalVfx;
            _sceneManager = Requires.IsNotNull(sceneManager, nameof(sceneManager));

            _effectLinkerFile = resourceProvider.GetGameResourceFile<EffectLinkerFile>(
                FileConstants.CombatDataFolderVirtualPath + EFFECT_LINKER_FILE_NAME);

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
            ClearSceneEffects();
        }

        public void Execute(EffectPreLoadCommand command)
        {
            // In PAL3A, PreLoad command is always issued right before the effect is played.
            // So it is pointless to pre-load the effect here for PAL3A.
            // Even with a waiter, we need to make sure waiter's current execution mode is set
            // to synchronous otherwise the effect will be played before the pre-load is finished.
            #if PAL3
            Pal3.Instance.StartCoroutine(_optionalVfx.PreloadAsync(command.EffectGroupId));
            #endif
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

            if (_effectPositionCommand is EffectSetPositionCommand positionCommand)
            {
                Vector3 localPosition = new GameBoxVector3(
                    positionCommand.GameBoxXPosition,
                    positionCommand.GameBoxYPosition,
                    positionCommand.GameBoxZPosition).ToUnityPosition();

                _optionalVfx.Play(command.EffectGroupId,
                    _sceneManager.GetSceneRootGameEntity(), localPosition);
            }
            else if (_effectPositionCommand is EffectAttachToActorCommand actorCommand)
            {
                _optionalVfx.Play(command.EffectGroupId,
                    currentScene.GetActorGameEntity(actorCommand.ActorId), Vector3.zero);
            }

            // Play SFX if any
            if (EffectConstants.EffectSfxInfo.TryGetValue(command.EffectGroupId, out var sfxName))
            {
                Pal3.Instance.Execute(
                    new PlaySfxCommand(sfxName, 1));
            }
        }

        #if PAL3A
        private void HandleNanGongHuangTransformEffect(EffectAttachToActorCommand effectAttachToActorCommand,
            EffectPlayCommand effectPlayCommand,
            GameScene currentScene)
        {
            int actorId = effectAttachToActorCommand.ActorId;
            switch (effectPlayCommand.EffectGroupId)
            {
                // 南宫煌切换为狼妖形态特效
                case 164:
                {
                    GameActor actor = currentScene.GetActor(actorId);
                    actor.ChangeName(ActorConstants.NanGongHuangWolfModeActorName);
                    if (actor.IsActive)
                    {
                        ActorActionController actorActionController = currentScene.GetActorGameEntity(actorId)
                            .GetComponent<ActorActionController>();
                        actorActionController.PerformAction(actor.GetIdleAction(), overwrite: true);
                    }
                    break;
                }
                // 南宫煌切换为人形态特效
                case 315:
                {
                    GameActor actor = currentScene.GetActor(actorId);
                    actor.ChangeName(ActorConstants.NanGongHuangHumanModeActorName);
                    if (actor.IsActive)
                    {
                        ActorActionController actorActionController = currentScene.GetActorGameEntity(actorId)
                            .GetComponent<ActorActionController>();
                        actorActionController.PerformAction(actor.GetIdleAction(), overwrite: true);
                    }
                    break;
                }
            }
        }
        #endif

        public void Execute(EffectSetPositionCommand command) => _effectPositionCommand = command;

        public void Execute(EffectAttachToActorCommand command) => _effectPositionCommand = command;

        public void Execute(SceneLeavingCurrentSceneNotification command) => ClearSceneEffects();

        public void Execute(ResetGameStateCommand command) => ClearSceneEffects();

        private void ClearSceneEffects()
        {
            _effectPositionCommand = null;
            _optionalVfx.ClearPlayedEffects();
        }
    }
}