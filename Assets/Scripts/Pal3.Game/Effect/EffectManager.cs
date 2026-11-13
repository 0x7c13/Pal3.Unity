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
        private Vector3 _originalPosition;

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
            if (_sceneManager.GetCurrentScene() is not { } currentScene)
                return;
#if PAL3A
            if (_effectPositionCommand is EffectAttachToActorCommand attach &&
                !currentScene.GetActorGameEntity(attach.ActorId).IsNullOrDisposed())
                HandleNanGongHuangTransformEffect(attach, command, currentScene);
#endif
            // Both games retain the last position when cmd_CEft_Role cannot find
            // its actor. Playback uses that snapshot even if the actor is gone.
            bool playedOriginal = _optionalVfx.Play(command.EffectGroupId,
                _sceneManager.GetSceneRootGameEntity(), _originalPosition,
                privateFallback: _effectPositionCommand is EffectAttachToActorCommand role
                    ? (currentScene.GetActorGameEntity(role.ActorId), Vector3.zero) : null);

            // Private fallback retains its old audio mapping; native groups own their audio tracks.
            if (!playedOriginal && EffectConstants.EffectSfxInfo.TryGetValue(command.EffectGroupId, out var sfxName))
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

        public void Execute(EffectSetPositionCommand command)
        {
            _effectPositionCommand = command;
            _originalPosition = new GameBoxVector3(command.GameBoxXPosition, command.GameBoxYPosition, command.GameBoxZPosition).ToUnityPosition();
        }

        public void Execute(EffectAttachToActorCommand command)
        {
            _effectPositionCommand = command;
            var scene = _sceneManager.GetCurrentScene();
            var actor = scene?.GetActorGameEntity(command.ActorId);
            if (!actor.IsNullOrDisposed())
            {
                var root = (UnityEngine.Transform)_sceneManager.GetSceneRootGameEntity().Transform.NativeObject;
                var transform = (UnityEngine.Transform)actor.Transform.NativeObject;
                // cmd_CEft_Role captures the position immediately and adds two
                // GameBox units. It does not attach the effect to a moving actor.
                _originalPosition = root.InverseTransformPoint(transform.position) + Vector3.up * 0.1f;
            }
        }

        public void Execute(SceneLeavingCurrentSceneNotification command) => ClearSceneEffects();

        public void Execute(ResetGameStateCommand command) => ClearSceneEffects();

        private void ClearSceneEffects()
        {
            _effectPositionCommand = null;
            _originalPosition = Vector3.zero;
            _optionalVfx.ClearPlayedEffects();
        }
    }
}
