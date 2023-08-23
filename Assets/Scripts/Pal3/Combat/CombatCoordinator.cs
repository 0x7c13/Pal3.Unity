// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Combat
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Actor.Controllers;
    using Audio;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.Contracts;
    using Core.DataReader.Nav;
    using Core.DataReader.Txt;
    using Core.Utils;
    using Data;
    using GamePlay;
    using MetaData;
    using Scene;
    using Script.Waiter;
    using Settings;
    using State;
    using UnityEngine;
    using Random = System.Random;

    /// <summary>
    /// Receive events and gather context to start a combat.
    /// </summary>
    public sealed class CombatCoordinator : IDisposable,
        ICommandExecutor<CombatEnterNormalFightCommand>,
        ICommandExecutor<CombatEnterBossFightCommand>,
        #if PAL3A
        ICommandExecutor<CombatEnterBossFightUsingMusicCommand>,
        ICommandExecutor<CombatEnterBossFightUsingMusicWithSpecialActorCommand>,
        #endif
        ICommandExecutor<CombatSetUnbeatableCommand>,
        ICommandExecutor<CombatSetMaxRoundCommand>,
        ICommandExecutor<CombatSetNoGameOverWhenLoseCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private const string COMBAT_SCN_FILE_NAME = "combatScn.txt";

        private readonly GameSettings _gameSettings;
        private readonly CombatManager _combatManager;
        private readonly PlayerActorManager _playerActorManager;
        private readonly AudioManager _audioManager;
        private readonly SceneManager _sceneManager;
        private readonly GameStateManager _gameStateManager;

        private readonly CombatScnFile _combatScnFile;
        private readonly CombatContext _currentCombatContext = new();

        public CombatCoordinator(GameResourceProvider resourceProvider,
            GameSettings gameSettings,
            CombatManager combatManager,
            PlayerActorManager playerActorManager,
            AudioManager audioManager,
            SceneManager sceneManager,
            GameStateManager gameStateManager)
        {
            Requires.IsNotNull(resourceProvider, nameof(resourceProvider));
            _gameSettings = Requires.IsNotNull(gameSettings, nameof(gameSettings));
            _combatManager = Requires.IsNotNull(combatManager, nameof(combatManager));
            _playerActorManager = Requires.IsNotNull(playerActorManager, nameof(playerActorManager));
            _audioManager = Requires.IsNotNull(audioManager, nameof(audioManager));
            _sceneManager = Requires.IsNotNull(sceneManager, nameof(sceneManager));
            _gameStateManager = Requires.IsNotNull(gameStateManager, nameof(gameStateManager));

            _combatScnFile = resourceProvider.GetGameResourceFile<CombatScnFile>(
                FileConstants.DataScriptFolderVirtualPath + COMBAT_SCN_FILE_NAME);

            _combatManager.OnCombatFinished += OnOnCombatFinished;
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            _combatManager.OnCombatFinished -= OnOnCombatFinished;
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        private void StartCombat()
        {
            if (!_gameSettings.IsTurnBasedCombatEnabled) return;

            Scene currentScene = _sceneManager.GetCurrentScene();

            // Figure out which combat scene to use
            string combatSceneName = GetCombatSceneName(currentScene);
            _currentCombatContext.SetCombatSceneName(combatSceneName);

            // Figure out element property of the combat scene
            if (_combatScnFile.CombatSceneElementTypeInfo.TryGetValue(combatSceneName,
                    out ElementType combatSceneElementType))
            {
                _currentCombatContext.SetCombatSceneElementType(combatSceneElementType);
            }

            // Figure out which combat music to play
            if (string.IsNullOrEmpty(_currentCombatContext.CombatMusicName))
            {
                if (MusicConstants.CombatMusicInfo.TryGetValue(
                        currentScene.GetSceneInfo().CityName.ToLower(),
                        out var combatMusic) &&
                    // Script can override combat music
                    string.IsNullOrEmpty(_audioManager.GetCurrentScriptMusic()))
                {
                    _currentCombatContext.SetCombatMusicName(combatMusic);
                }
            }

            // Check current state and add script waiter to pause script execution
            // if we are in cutscene state.
            if (_gameStateManager.GetCurrentState() == GameState.Cutscene)
            {
                var combatWaiter = new WaitUntilCanceled();
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ScriptRunnerAddWaiterRequest(combatWaiter));
                _currentCombatContext.SetScriptWaiter(combatWaiter);
            }

            // Start combat
            Debug.LogWarning($"[{nameof(CombatCoordinator)}] Starting combat with context: {_currentCombatContext}");
            _gameStateManager.TryGoToState(GameState.Combat);
            _combatManager.EnterCombat(_currentCombatContext);
        }

        private void OnOnCombatFinished(object sender, bool isPlayerWin)
        {
            _combatManager.ExitCombat();
            _currentCombatContext.ScriptWaiter?.CancelWait();

            if (isPlayerWin)
            {
                _gameStateManager.GoToPreviousState();
                _currentCombatContext.ResetContext();
            }
            else
            {
                _currentCombatContext.ResetContext();
                CommandDispatcher<ICommand>.Instance.Dispatch(new GameSwitchToMainMenuCommand());
            }
        }

        private string GetCombatSceneName(Scene currentScene)
        {
            var sceneInfo = currentScene.GetSceneInfo();

            if (!_combatScnFile.CombatSceneMapInfo.TryGetValue($"{sceneInfo.CityName}_{sceneInfo.SceneName}",
                    out var combatSceneFloorKindToMapInfo))
            {
                throw new DataException($"Combat scene not found for {sceneInfo.CityName}_{sceneInfo.SceneName}!");
            }

            var floorType = FloorType.Default;

            PlayerActorId actorId = _playerActorManager.GetPlayerActor();
            GameObject playerActorGo = currentScene.GetActorGameObject((int) actorId);
            var playerActorMovementController = playerActorGo.GetComponent<ActorMovementController>();
            Vector2Int playerActorTilePosition = playerActorMovementController.GetTilePosition();
            int playerActorLayerIndex = playerActorMovementController.GetCurrentLayerIndex();

            Tilemap tileMap = currentScene.GetTilemap();
            if (tileMap.TryGetTile(playerActorTilePosition, playerActorLayerIndex, out NavTile tile))
            {
                floorType = tile.FloorType;
            }

            if (!combatSceneFloorKindToMapInfo.TryGetValue(floorType, out var combatSceneName))
            {
                combatSceneName = combatSceneFloorKindToMapInfo[FloorType.Default];
            }

            #if PAL3
            combatSceneName = combatSceneName.ToUpper();
            #endif

            return combatSceneName;
        }

        public void Execute(CombatEnterNormalFightCommand command)
        {
            uint[] availableIds = new HashSet<uint>()
            {
                command.Monster1Id,
                command.Monster2Id,
                command.Monster3Id,
            }.Where(id => id != 0).ToArray();

            if (availableIds.Length == 0)
            {
                throw new ArgumentException("At least one monster id must be specified!");
            }

            uint[] monsterIds = new uint[6];
            Random rand = new Random();

            // Use each available ID at least once.
            for (var i = 0; i < availableIds.Length; i++)
            {
                monsterIds[i] = availableIds[i];
            }

            // Randomly select from available IDs to populate remaining monsterIds.
            for (var i = availableIds.Length; i < command.NumberOfMonster; i++)
            {
                int randomIndex = rand.Next(0, availableIds.Length);
                monsterIds[i] = availableIds[randomIndex];
            }

            // Shuffle the array
            for (var i = 0; i < monsterIds.Length; i++)
            {
                int randomIndex = rand.Next(i, monsterIds.Length);
                (monsterIds[i], monsterIds[randomIndex]) = (monsterIds[randomIndex], monsterIds[i]);
            }

            _currentCombatContext.SetMonsterIds(
                monsterIds[0],
                monsterIds[1],
                monsterIds[2],
                monsterIds[3],
                monsterIds[4],
                monsterIds[5]);
            StartCombat();
        }

        public void Execute(CombatEnterBossFightCommand command)
        {
            _currentCombatContext.SetMonsterIds(
                command.Monster1Id,
                command.Monster2Id,
                command.Monster3Id,
                command.Monster4Id,
                command.Monster5Id,
                command.Monster6Id);
            StartCombat();
        }

        #if PAL3A
        public void Execute(CombatEnterBossFightUsingMusicCommand command)
        {
            _currentCombatContext.SetMonsterIds(
                command.Monster1Id,
                command.Monster2Id,
                command.Monster3Id,
                command.Monster4Id,
                command.Monster5Id,
                command.Monster6Id);
            _currentCombatContext.SetCombatMusicName(command.CombatMusicName);
            StartCombat();
        }

        public void Execute(CombatEnterBossFightUsingMusicWithSpecialActorCommand command)
        {
            _currentCombatContext.SetMonsterIds(
                command.Monster1Id,
                command.Monster2Id,
                command.Monster3Id,
                command.Monster4Id,
                command.Monster5Id,
                command.Monster6Id);
            _currentCombatContext.SetCombatMusicName(command.CombatMusicName);
            // TODO: NanGoongHuang enter fight using wolf state
            StartCombat();
        }
        #endif

        public void Execute(CombatSetUnbeatableCommand command)
        {
            _currentCombatContext.SetUnbeatable(true);
        }

        public void Execute(CombatSetMaxRoundCommand command)
        {
            _currentCombatContext.SetMaxRound(command.MaxRound);
        }

        public void Execute(CombatSetNoGameOverWhenLoseCommand command)
        {
            _currentCombatContext.SetNoGameOverWhenLose(true);
        }

        public void Execute(ResetGameStateCommand command)
        {
            _currentCombatContext.ResetContext();
        }
    }
}