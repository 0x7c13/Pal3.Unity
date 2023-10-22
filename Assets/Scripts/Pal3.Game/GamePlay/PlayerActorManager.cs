// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.GamePlay
{
    using System;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Enums;

    using Vector2Int = UnityEngine.Vector2Int;
    using Vector3 = UnityEngine.Vector3;

    public sealed class PlayerActorManager : IDisposable,
        ICommandExecutor<ActorEnablePlayerControlCommand>,
        ICommandExecutor<PlayerEnableInputCommand>,
        ICommandExecutor<ScenePreLoadingNotification>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private PlayerActorId _playerActor = 0;
        private bool _playerActorControlEnabled = true;
        private bool _playerInputEnabled;

        public Vector3? LastKnownPosition { get; set; }
        public Vector2Int? LastKnownTilePosition { get; set; }
        public int? LastKnownLayerIndex { get; set; }

        public PlayerActorManager()
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

        public void Execute(ScenePreLoadingNotification command)
        {
            // Need to set main actor as player actor for non-maze scenes
            if (command.NewSceneInfo.SceneType != SceneType.Maze && _playerActor != 0)
            {
                _playerActor = 0;
            }
        }

        public void Execute(ResetGameStateCommand command)
        {
            _playerActor = 0;
            _playerActorControlEnabled = true;
            _playerInputEnabled = false;

            LastKnownPosition = null;
            LastKnownTilePosition = null;
            LastKnownLayerIndex = null;
        }

        public void Execute(ActorEnablePlayerControlCommand command)
        {
            if (Enum.IsDefined(typeof(PlayerActorId), command.ActorId))
            {
                _playerActor = (PlayerActorId)command.ActorId;
                _playerActorControlEnabled = true;
            }
        }
    }
}