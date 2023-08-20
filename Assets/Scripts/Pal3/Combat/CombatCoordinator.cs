// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Combat
{
    using System;
    using System.Data;
    using Actor.Controllers;
    using Command;
    using Command.SceCommands;
    using Core.DataReader.Nav;
    using Core.DataReader.Scn;
    using Core.DataReader.Txt;
    using Core.Utils;
    using Data;
    using GamePlay;
    using MetaData;
    using Scene;
    using UnityEngine;

    /// <summary>
    /// Receive events and gather context to start a combat.
    /// </summary>
    public sealed class CombatCoordinator : IDisposable,
        ICommandExecutor<CombatEnterNormalFightCommand>,
        ICommandExecutor<CombatEnterBossFightCommand>,
        #if PAL3A
        ICommandExecutor<CombatEnterBossFightUsingMusicCommand>,
        #endif
        ICommandExecutor<CombatSetUnwinnableCommand>,
        ICommandExecutor<CombatSetMaxRoundCommand>,
        ICommandExecutor<CombatSetNoGameOverCommand>
    {
        private const string COMBAT_SCN_FILE_NAME = "combatScn.txt";

        private readonly CombatScnFile _combatScnFile;
        private readonly PlayerActorManager _playerActorManager;
        private readonly SceneManager _sceneManager;

        public CombatCoordinator(GameResourceProvider resourceProvider,
            PlayerActorManager playerActorManager,
            SceneManager sceneManager)
        {
            Requires.IsNotNull(resourceProvider, nameof(resourceProvider));
            _playerActorManager = Requires.IsNotNull(playerActorManager, nameof(playerActorManager));
            _sceneManager = Requires.IsNotNull(sceneManager, nameof(sceneManager));

            _combatScnFile = resourceProvider.GetGameResourceFile<CombatScnFile>(
                FileConstants.DataScriptFolderVirtualPath + COMBAT_SCN_FILE_NAME);

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        private void StartCombat()
        {
            // Figure out which combat scene to use
            var combatSceneName = GetCombatSceneName();
            Debug.LogWarning(combatSceneName);

            // Figure out enemy actor ids and their positions

            // Figure out player actor ids and their positions
        }

        private string GetCombatSceneName()
        {
            ScnSceneInfo sceneInfo = _sceneManager.GetCurrentScene().GetSceneInfo();

            if (_combatScnFile.CombatSceneMapInfo.TryGetValue($"{sceneInfo.CityName}_{sceneInfo.SceneName}",
                    out var combatSceneFloorKindToMapInfo))
            {
                var floorKind = NavFloorKind.Default;

                PlayerActorId actorId = _playerActorManager.GetPlayerActor();
                Scene currentScene = _sceneManager.GetCurrentScene();
                GameObject playerActorGo = currentScene.GetActorGameObject((int) actorId);
                var playerActorMovementController = playerActorGo.GetComponent<ActorMovementController>();
                Vector2Int playerActorTilePosition = playerActorMovementController.GetTilePosition();
                int playerActorLayerIndex = playerActorMovementController.GetCurrentLayerIndex();

                Tilemap tileMap = currentScene.GetTilemap();
                if (tileMap.TryGetTile(playerActorTilePosition, playerActorLayerIndex, out NavTile tile))
                {
                    floorKind = tile.FloorKind;
                }

                if (!combatSceneFloorKindToMapInfo.TryGetValue(floorKind, out var combatSceneName))
                {
                    combatSceneName = combatSceneFloorKindToMapInfo[NavFloorKind.Default];
                }

                return combatSceneName;
            }
            else
            {
                throw new DataException("Combat scene not found!");
            }
        }

        public void Execute(CombatEnterNormalFightCommand command)
        {
            Debug.LogWarning(command.NumberOfMonster + " " +
                           command.Monster1Id + " " +
                           command.Monster2Id + " " +
                           command.Monster3Id);
            StartCombat();
        }

        public void Execute(CombatEnterBossFightCommand command)
        {
            StartCombat();
        }

        #if PAL3A
        public void Execute(CombatEnterBossFightUsingMusicCommand command)
        {
            StartCombat();
        }
        #endif

        public void Execute(CombatSetUnwinnableCommand command)
        {
        }

        public void Execute(CombatSetMaxRoundCommand command)
        {
        }

        public void Execute(CombatSetNoGameOverCommand command)
        {
        }
    }
}