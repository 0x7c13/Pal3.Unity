// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Player
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Actor;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataReader.Scn;
    using MetaData;
    using Scene;
    using UnityEngine;

    public class TeamManager :
        ICommandExecutor<TeamOpenCommand>,
        ICommandExecutor<TeamCloseCommand>,
        ICommandExecutor<TeamAddOrRemoveActorCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private const float TEAM_OPEN_SPAWN_POINT_PLAYER_OFFSET = 2.5f;

        private readonly PlayerManager _playerManager;
        private readonly SceneManager _sceneManager;

        private readonly HashSet<PlayerActorId> _actorsInTeam = new ();

        private static readonly float[] ActorSpawnPositionDirectionInDegrees =
        {
            40f,
            -40f,
            0f,
            -80f,
            80f,
        };

        public TeamManager(PlayerManager playerManager, SceneManager sceneManager)
        {
            _playerManager = playerManager;
            _sceneManager = sceneManager;
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public bool IsActorInTeam(PlayerActorId actorId)
        {
            return _actorsInTeam.Contains(actorId);
        }

        public HashSet<PlayerActorId> GetActorsInTeam()
        {
            return _actorsInTeam;
        }

        public void Execute(TeamOpenCommand command)
        {
            var playerActorId = _playerManager.GetPlayerActor();
            var playerActor = _sceneManager.GetCurrentScene().GetActorGameObject((byte)playerActorId);
            var currentNavLayer = playerActor.GetComponent<ActorMovementController>().GetCurrentLayerIndex();

            var tilemap = _sceneManager.GetCurrentScene().GetTilemap();

            var index = 0;

            #if PAL3
            foreach (var actor in _actorsInTeam.Where(a => a != playerActorId && a != PlayerActorId.HuaYing))
            #elif  PAL3A
            foreach (var actor in _actorsInTeam.Where(a => a != playerActorId && a != PlayerActorId.TaoZi))
            #endif
            {
                var actorObject = _sceneManager.GetCurrentScene().GetActorGameObject((byte)actor);
                actorObject.GetComponent<ActorController>().IsActive = true;
                var spawnPosition = CalculateSpawnPosition(playerActor.transform, index);
                var tilePosition = tilemap.GetTilePosition(spawnPosition, currentNavLayer);
                actorObject.transform.position = tilemap.GetWorldPosition(tilePosition, currentNavLayer);
                var actorMovementController = actorObject.GetComponent<ActorMovementController>();
                actorMovementController.SetNavLayer(currentNavLayer);
                index++;
            }

            #if PAL3
            if (IsActorInTeam(PlayerActorId.XueJian))
            {
                var huaYing = _sceneManager.GetCurrentScene().GetActorGameObject((byte)PlayerActorId.HuaYing);
                huaYing.GetComponent<ActorMovementController>().SetNavLayer(currentNavLayer);
            }
            #endif
        }

        private Vector3 CalculateSpawnPosition(Transform playerActorTransform, int indexInTeam)
        {
            var directionInDegrees = ActorSpawnPositionDirectionInDegrees[indexInTeam];
            return playerActorTransform.position +
                   Quaternion.AngleAxis(directionInDegrees, Vector3.up) *
                   -playerActorTransform.forward *
                   TEAM_OPEN_SPAWN_POINT_PLAYER_OFFSET;
        }

        public void Execute(TeamCloseCommand command)
        {
            var playerActorId = _playerManager.GetPlayerActor();

            #if PAL3A
            // Need to add all active player actors into the team
            if (_sceneManager.GetCurrentScene().GetSceneInfo().SceneType == ScnSceneType.Maze)
            {
                var playerActorIds = Enum.GetValues(typeof(PlayerActorId)).Cast<int>();
                var activePlayerActors = _sceneManager.GetCurrentScene()
                    .GetAllActors()
                    .Where(actor => playerActorIds.Contains(actor.Key) &&
                                    actor.Value.GetComponent<ActorController>().IsActive);
                foreach (var activePlayerActor in activePlayerActors)
                {
                    AddActor((PlayerActorId)activePlayerActor.Key);
                }
            }
            #endif

            foreach (var actor in _actorsInTeam.Where(a => a != playerActorId))
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new ActorActivateCommand((int)actor, 0));
            }
        }

        public void AddActor(PlayerActorId actorId)
        {
            if (!_actorsInTeam.Contains(actorId))
            {
                _actorsInTeam.Add(actorId);
            }
        }

        public void RemoveActor(PlayerActorId actorId)
        {
            if (_actorsInTeam.Contains(actorId))
            {
                _actorsInTeam.Remove(actorId);
            }
        }

        public void Execute(TeamAddOrRemoveActorCommand command)
        {
            var actor = (PlayerActorId) command.ActorId;

            if (command.IsIn == 1)
            {
                AddActor(actor);
            }
            else
            {
                RemoveActor(actor);
            }
        }

        public void Execute(ResetGameStateCommand command)
        {
            _actorsInTeam.Clear();
        }
    }
}