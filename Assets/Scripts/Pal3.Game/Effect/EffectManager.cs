// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

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
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
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
        private readonly GameResourceProvider _resourceProvider;
        private readonly SceneManager _sceneManager;
        private ICommand _effectPositionCommand;

        private const string EFFECT_LINKER_FILE_NAME = "efflinker.dat";
        private EffectLinkerFile _effectLinkerFile;

        public EffectManager(GameResourceProvider resourceProvider,
            SceneManager sceneManager)
        {
            _resourceProvider = Requires.IsNotNull(resourceProvider, nameof(resourceProvider));
            _sceneManager = Requires.IsNotNull(sceneManager, nameof(sceneManager));

            _effectLinkerFile = _resourceProvider.GetGameResourceFile<EffectLinkerFile>(
                FileConstants.CombatDataFolderVirtualPath + EFFECT_LINKER_FILE_NAME);

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
            _effectPositionCommand = null;
        }

        public void Execute(EffectPreLoadCommand command)
        {
            // In PAL3A, PreLoad command is always issued right before the effect is played.
            // So it is pointless to pre-load the effect here for PAL3A.
            // Even with a waiter, we need to make sure waiter's current execution mode is set
            // to synchronous otherwise the effect will be played before the pre-load is finished.
            #if PAL3
            Pal3.Instance.StartCoroutine(_resourceProvider.PreLoadVfxEffectAsync(command.EffectGroupId));
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

            // Play VFX
            object vfxPrefab = _resourceProvider.GetVfxEffectPrefab(command.EffectGroupId);
            if (vfxPrefab != null)
            {
                IGameEntity parent = null;
                Vector3 localPosition = Vector3.zero;

                if (_effectPositionCommand is EffectSetPositionCommand positionCommand &&
                    _sceneManager.GetSceneRootGameEntity() is {} sceneRootGameEntity)
                {
                    parent = sceneRootGameEntity;
                    localPosition = new GameBoxVector3(
                            positionCommand.GameBoxXPosition,
                            positionCommand.GameBoxYPosition,
                            positionCommand.GameBoxZPosition).ToUnityPosition();
                }
                else if (_effectPositionCommand is EffectAttachToActorCommand actorCommand)
                {
                    parent = currentScene.GetActorGameEntity(actorCommand.ActorId);
                }

                if (parent != null)
                {
                    IGameEntity effectEntity = GameEntityFactory.Create(name: $"VFX_{command.EffectGroupId}",
                        vfxPrefab, parent, worldPositionStays: false);
                    effectEntity.Transform.LocalPosition += localPosition;
                }
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
            Scene currentScene)
        {
            var actorId = effectAttachToActorCommand.ActorId;
            switch (effectPlayCommand.EffectGroupId)
            {
                // 南宫煌切换为狼妖形态特效
                case 164:
                {
                    Actor actor = currentScene.GetActor(actorId);
                    actor.ChangeName(ActorConstants.NanGongHuangWolfModeActorName);
                    if (actor.IsActive)
                    {
                        var actorActionController = currentScene.GetActorGameEntity(actorId)
                            .GetComponent<ActorActionController>();
                        actorActionController.PerformAction(actor.GetIdleAction(), overwrite: true);
                    }
                    break;
                }
                // 南宫煌切换为人形态特效
                case 315:
                {
                    Actor actor = currentScene.GetActor(actorId);
                    actor.ChangeName(ActorConstants.NanGongHuangHumanModeActorName);
                    if (actor.IsActive)
                    {
                        var actorActionController = currentScene.GetActorGameEntity(actorId)
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

        public void Execute(SceneLeavingCurrentSceneNotification command) => _effectPositionCommand = null;

        public void Execute(ResetGameStateCommand command) => _effectPositionCommand = null;
    }
}