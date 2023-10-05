// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.GameSystems.Team
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Actor.Controllers;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Enums;
    using Core.Utilities;
    using Engine.Logging;
    using GamePlay;
    using Scene;
    using UnityEngine;

    public sealed class TeamManager : IDisposable,
        ICommandExecutor<TeamOpenCommand>,
        ICommandExecutor<TeamCloseCommand>,
        ICommandExecutor<TeamAddOrRemoveActorCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private const float TEAM_OPEN_SPAWN_POINT_PLAYER_OFFSET = 2.5f;

        private readonly PlayerActorManager _playerActorManager;
        private readonly SceneManager _sceneManager;

        private readonly HashSet<PlayerActorId> _actorsInTeam = new ();

        #if PAL3
        private static readonly float[] ActorSpawnPositionDirectionInDegrees =
        {
            40f,
            -40f,
            0f,
            -80f,
            80f,
        };
        #elif PAL3A
        private static readonly float[] ActorSpawnPositionDirectionInDegrees =
        {
            -40f,
            40f,
            0f,
            -80f,
            80f,
        };
        #endif

        public TeamManager(PlayerActorManager playerActorManager, SceneManager sceneManager)
        {
            _playerActorManager = Requires.IsNotNull(playerActorManager, nameof(playerActorManager));
            _sceneManager = Requires.IsNotNull(sceneManager, nameof(sceneManager));
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
            PlayerActorId playerActorId = _playerActorManager.GetPlayerActor();
            GameObject playerActor = _sceneManager.GetCurrentScene().GetActorGameObject((int)playerActorId);
            var currentNavLayer = playerActor.GetComponent<ActorMovementController>().GetCurrentLayerIndex();

            Tilemap tilemap = _sceneManager.GetCurrentScene().GetTilemap();

            var index = 0;

            #if PAL3
            foreach (PlayerActorId actor in _actorsInTeam.Where(a => a != playerActorId && a != PlayerActorId.HuaYing))
            #elif  PAL3A
            foreach (PlayerActorId actor in _actorsInTeam.Where(a => a != playerActorId && a != PlayerActorId.TaoZi))
            #endif
            {
                GameObject actorObject = _sceneManager.GetCurrentScene().GetActorGameObject((int)actor);
                actorObject.GetComponent<ActorController>().IsActive = true;
                Vector3 spawnPosition = CalculateSpawnPosition(playerActor.transform, index);
                Vector2Int tilePosition = tilemap.GetTilePosition(spawnPosition, currentNavLayer);
                actorObject.transform.position = tilemap.GetWorldPosition(tilePosition, currentNavLayer);
                var actorMovementController = actorObject.GetComponent<ActorMovementController>();
                actorMovementController.SetNavLayer(currentNavLayer);
                index++;
            }

            #if PAL3
            if (IsActorInTeam(PlayerActorId.XueJian))
            {
                GameObject huaYing = _sceneManager.GetCurrentScene().GetActorGameObject((int)PlayerActorId.HuaYing);
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
            PlayerActorId playerActorId = _playerActorManager.GetPlayerActor();

            #if PAL3A
            // Need to add all active player actors into the team within certain radius
            {
                GameObject playerActor = _sceneManager.GetCurrentScene().GetActorGameObject((int)playerActorId);
                Vector3 playerActorPosition = playerActor.transform.position;

                var playerActorIds = Enum.GetValues(typeof(PlayerActorId)).Cast<int>();
                var activePlayerActors = _sceneManager.GetCurrentScene()
                    .GetAllActorGameObjects()
                    .Where(actor => playerActorIds.Contains(actor.Key) &&
                                    actor.Value.GetComponent<ActorController>().IsActive);

                foreach (var activePlayerActor in activePlayerActors)
                {
                    if (Vector3.Distance(playerActorPosition,
                            activePlayerActor.Value.transform.position) < 13f) // 13f is about right
                    {
                        AddActor((PlayerActorId)activePlayerActor.Key);
                    }
                }
            }
            #endif

            foreach (PlayerActorId actor in _actorsInTeam.Where(a => a != playerActorId))
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
            if (!Enum.IsDefined(typeof(PlayerActorId), command.ActorId))
            {
                EngineLogger.LogError("Cannot add non-player actor to the team");
                return;
            }

            PlayerActorId actor = (PlayerActorId) command.ActorId;

            #if PAL3
            if (actor == PlayerActorId.HuaYing)
            #elif PAL3A
            if (actor == PlayerActorId.TaoZi)
            #endif
            {
                EngineLogger.LogError($"{actor} cannot be added to the team");
                return;
            }

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