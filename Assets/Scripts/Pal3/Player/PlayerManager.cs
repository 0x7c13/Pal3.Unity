// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Player
{
    using System;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataReader.Scn;
    using MetaData;

    public sealed class PlayerManager : IDisposable,
        ICommandExecutor<DialogueRenderActorAvatarCommand>,
        ICommandExecutor<ActorActivateCommand>,
        ICommandExecutor<ActorAddSkillCommand>,
        ICommandExecutor<ActorAutoStandCommand>,
        ICommandExecutor<ActorPerformActionCommand>,
        ICommandExecutor<ActorChangeScaleCommand>,
        ICommandExecutor<ActorChangeTextureCommand>,
        ICommandExecutor<ActorSetTilePositionCommand>,
        ICommandExecutor<ActorSetWorldPositionCommand>,
        ICommandExecutor<ActorChangeColliderSettingCommand>,
        ICommandExecutor<ActorSetFacingDirectionCommand>,
        ICommandExecutor<ActorRotateFacingCommand>,
        ICommandExecutor<ActorRotateFacingDirectionCommand>,
        ICommandExecutor<ActorPathToCommand>,
        #if PAL3A
        ICommandExecutor<ActorWalkToUsingActionCommand>,
        #endif
        ICommandExecutor<ActorMoveToCommand>,
        ICommandExecutor<ActorMoveBackwardsCommand>,
        ICommandExecutor<ActorMoveOutOfScreenCommand>,
        ICommandExecutor<ActorLookAtActorCommand>,
        ICommandExecutor<ActorShowEmojiCommand>,
        #if PAL3A
        ICommandExecutor<ActorShowEmoji2Command>,
        #endif
        ICommandExecutor<ActorStopActionCommand>,
        ICommandExecutor<ActorStopActionAndStandCommand>,
        ICommandExecutor<ActorEnablePlayerControlCommand>,
        ICommandExecutor<ActorSetNavLayerCommand>,
        ICommandExecutor<ActorFadeInCommand>,
        ICommandExecutor<ActorFadeOutCommand>,
        ICommandExecutor<ActorSetScriptCommand>,
        #if PAL3A
        ICommandExecutor<ActorSetYPositionCommand>,
        #endif
        ICommandExecutor<PlayerEnableInputCommand>,
        ICommandExecutor<CameraFocusOnActorCommand>,
        ICommandExecutor<EffectAttachToActorCommand>,
        ICommandExecutor<ScenePreLoadingNotification>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private PlayerActorId _playerActor = 0;
        private bool _playerActorControlEnabled = true;
        private bool _playerInputEnabled;

        public PlayerManager()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public PlayerActorId GetPlayerActor()
        {
            return _playerActor;
        }

        public bool IsPlayerActorControlEnabled()
        {
            return _playerActorControlEnabled;
        }

        public bool IsPlayerInputEnabled()
        {
            return _playerInputEnabled;
        }

        public void Execute(PlayerEnableInputCommand command)
        {
            _playerInputEnabled = (command.Enable == 1);
        }

        public void Execute(DialogueRenderActorAvatarCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new DialogueRenderActorAvatarCommand((int)_playerActor, command.AvatarTextureName, command.RightAligned));
            }
        }

        public void Execute(ActorActivateCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorActivateCommand((int)_playerActor, command.IsActive));
            }
        }

        public void Execute(ActorAddSkillCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorAddSkillCommand((int)_playerActor, command.SkillId));
            }
        }
        
        public void Execute(ActorAutoStandCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorAutoStandCommand((int)_playerActor, command.AutoStand));
            }
        }

        public void Execute(ActorPerformActionCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorPerformActionCommand((int)_playerActor, command.ActionName, command.LoopCount));
            }
        }

        public void Execute(ActorChangeScaleCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorChangeScaleCommand((int)_playerActor, command.Scale));
            }
        }
        
        public void Execute(ActorSetTilePositionCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorSetTilePositionCommand((int)_playerActor, command.TileXPosition, command.TileYPosition));
            }
        }

        public void Execute(ActorSetWorldPositionCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorSetWorldPositionCommand((int)_playerActor, command.XPosition, command.ZPosition));
            }
        }
        
        public void Execute(ActorChangeColliderSettingCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorChangeColliderSettingCommand((int)_playerActor, command.DisableCollider));
            }
        }

        public void Execute(ActorSetFacingDirectionCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorSetFacingDirectionCommand((int)_playerActor, command.Direction));
            }
        }

        public void Execute(ActorRotateFacingCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorRotateFacingCommand((int)_playerActor, command.Degrees));
            }
        }

        public void Execute(ActorRotateFacingDirectionCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorRotateFacingDirectionCommand((int)_playerActor, command.Direction));
            }
        }

        public void Execute(ActorPathToCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorPathToCommand((int)_playerActor, command.TileXPosition, command.TileYPosition, command.Mode));
            }
        }
        
        #if PAL3A
        public void Execute(ActorWalkToUsingActionCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorWalkToUsingActionCommand((int)_playerActor, command.TileXPosition, command.TileYPosition, command.Action));
            }
        }
        #endif
        
        public void Execute(ActorMoveToCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorMoveToCommand((int)_playerActor, command.TileXPosition, command.TileYPosition, command.Mode));
            }
        }
        
        public void Execute(ActorMoveOutOfScreenCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorMoveOutOfScreenCommand((int)_playerActor, command.TileXPosition, command.TileYPosition, command.Mode));
            }
        }

        public void Execute(ActorMoveBackwardsCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorMoveBackwardsCommand((int)_playerActor, command.GameBoxDistance));
            }
        }

        public void Execute(ActorLookAtActorCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorLookAtActorCommand((int)_playerActor, command.LookAtActorId));
            }
            else if (command.LookAtActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorLookAtActorCommand(command.ActorId, (int)_playerActor));
            }
        }

        public void Execute(ActorShowEmojiCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorShowEmojiCommand((int)_playerActor, command.EmojiId));
            }
        }
        
        #if PAL3A
        public void Execute(ActorShowEmoji2Command command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorShowEmoji2Command((int)_playerActor, command.EmojiId));
            }
        }
        #endif

        public void Execute(ActorFadeInCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorFadeInCommand((int)_playerActor));
            }
        }

        public void Execute(ActorFadeOutCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorFadeOutCommand((int)_playerActor));
            }
        }

        public void Execute(ActorChangeTextureCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorChangeTextureCommand((int)_playerActor, command.TextureName));
            }
        }

        public void Execute(ActorStopActionCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorStopActionCommand((int)_playerActor));
            }
        }

        public void Execute(ActorStopActionAndStandCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorStopActionAndStandCommand((int)_playerActor));
            }
        }

        public void Execute(ActorEnablePlayerControlCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorEnablePlayerControlCommand((int)_playerActor));
            }
            else if (Enum.IsDefined(typeof(PlayerActorId), command.ActorId))
            {
                _playerActor = (PlayerActorId)command.ActorId;
                _playerActorControlEnabled = true;
            }
        }
        
        public void Execute(ActorSetScriptCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorSetScriptCommand((int)_playerActor, command.ScriptId));
            }
        }

        public void Execute(ActorSetNavLayerCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorSetNavLayerCommand((int)_playerActor, command.LayerIndex));
            }
        }
        
        #if PAL3A
        public void Execute(ActorSetYPositionCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorSetYPositionCommand((int)_playerActor, command.GameBoxYPosition));
            }
        }
        #endif

        public void Execute(CameraFocusOnActorCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new CameraFocusOnActorCommand((int)_playerActor));
            }
        }

        public void Execute(EffectAttachToActorCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new EffectAttachToActorCommand((int)_playerActor));
            }
        }

        public void Execute(ScenePreLoadingNotification command)
        {
            // Need to set main actor as player actor for non-maze scenes
            if (command.NewSceneInfo.SceneType != ScnSceneType.Maze &&
                _playerActor != 0)
            {
                _playerActor = 0;
            }
        }
        
        public void Execute(ResetGameStateCommand command)
        {
            _playerActor = 0;
            _playerActorControlEnabled = true;
            _playerInputEnabled = false;
        }
    }
}