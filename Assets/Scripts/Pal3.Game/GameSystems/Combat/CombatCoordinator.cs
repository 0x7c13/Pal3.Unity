// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.GameSystems.Combat
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Audio;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Nav;
    using Core.DataReader.Scn;
    using Core.DataReader.Txt;
    using Core.Utilities;
    using Data;
    using Engine.Core.Abstraction;
    using Engine.Logging;
    using Game.Actor;
    using Game.Actor.Controllers;
    using Game.Scene;
    using GamePlay;
    using Script.Waiter;
    using Settings;
    using State;

    using Vector2Int = UnityEngine.Vector2Int;
    using Vector3 = UnityEngine.Vector3;

    /// <summary>
    /// Receive events and gather context to start a combat.
    /// </summary>
    public sealed class CombatCoordinator : IDisposable,
        ICommandExecutor<CombatActorCollideWithPlayerActorNotification>,
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
        private readonly CombatContextBuilder _combatContextBuilder = new();

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

            _combatManager.OnCombatFinished += OnCombatFinished;
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            _combatManager.OnCombatFinished -= OnCombatFinished;
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        private void StartCombat()
        {
            if (!_gameSettings.IsTurnBasedCombatEnabled) return;

            GameScene currentScene = _sceneManager.GetCurrentScene();

            // Figure out which combat scene to use
            string combatSceneName = GetCombatSceneName(currentScene);
            _combatContextBuilder.WithCombatSceneName(combatSceneName);

            // Figure out element property of the combat scene
            if (_combatScnFile.CombatSceneElementTypeInfo.TryGetValue(combatSceneName,
                    out ElementType combatSceneElementType))
            {
                _combatContextBuilder.WithCombatSceneElementType(combatSceneElementType);
            }

            // Figure out which combat music to play
            if (string.IsNullOrEmpty(_combatContextBuilder.CurrentContext.CombatMusicName))
            {
                if (MusicConstants.CombatMusicInfo.TryGetValue(
                        currentScene.GetSceneInfo().CityName.ToLower(),
                        out string combatMusic))
                {
                    // Set music only if it's not set yet or it's not a script triggered combat
                    if (string.IsNullOrEmpty(_audioManager.GetCurrentScriptMusic()) ||
                        !_combatContextBuilder.CurrentContext.IsScriptTriggeredCombat)
                    {
                        _combatContextBuilder.WithCombatMusicName(combatMusic);
                    }
                }
            }

            // Check current state and add script waiter to pause script execution
            // if we are in cutscene state.
            if (_gameStateManager.GetCurrentState() == GameState.Cutscene)
            {
                WaitUntilCanceled combatWaiter = new();
                Pal3.Instance.Execute(new ScriptRunnerAddWaiterRequest(combatWaiter));
                _combatContextBuilder.WithScriptWaiter(combatWaiter);
            }

            CombatContext combatContext = _combatContextBuilder.Build();

            // Start combat
            EngineLogger.LogWarning($"Starting combat with context: {combatContext}");
            _gameStateManager.TryGoToState(GameState.Combat);
            _combatManager.EnterCombat(combatContext);
        }

        private void OnCombatFinished(object sender, CombatResult combatResult)
        {
            _combatManager.ExitCombat();
            combatResult.CombatContext.ScriptWaiter?.CancelWait();
            _combatContextBuilder.ResetContext();

            if (combatResult.IsPlayerWin)
            {
                _gameStateManager.GoToPreviousState();
            }
            else
            {
                Pal3.Instance.Execute(new GameSwitchToMainMenuCommand());
            }
        }

        private string GetCombatSceneName(GameScene currentScene)
        {
            ScnSceneInfo sceneInfo = currentScene.GetSceneInfo();

            if (!_combatScnFile.CombatSceneMapInfo.TryGetValue($"{sceneInfo.CityName}_{sceneInfo.SceneName}",
                    out Dictionary<FloorType, string> combatSceneFloorKindToMapInfo))
            {
                throw new DataException($"Combat scene not found for {sceneInfo.CityName}_{sceneInfo.SceneName}!");
            }

            FloorType floorType = FloorType.Default;

            int actorId = _playerActorManager.GetPlayerActorId();
            IGameEntity playerActorEntity = currentScene.GetActorGameEntity(actorId);
            ActorMovementController playerActorMovementController = playerActorEntity.GetComponent<ActorMovementController>();
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

            // Use each available ID at least once.
            for (int i = 0; i < availableIds.Length; i++)
            {
                monsterIds[i] = availableIds[i];
            }

            // Randomly select from available IDs to populate remaining monsterIds.
            for (int i = availableIds.Length; i < command.NumberOfMonster; i++)
            {
                int randomIndex = RandomGenerator.Range(0, availableIds.Length);
                monsterIds[i] = availableIds[randomIndex];
            }

            // Shuffle the array
            for (int i = 0; i < monsterIds.Length; i++)
            {
                int randomIndex = RandomGenerator.Range(i, monsterIds.Length);
                (monsterIds[i], monsterIds[randomIndex]) = (monsterIds[randomIndex], monsterIds[i]);
            }

            _combatContextBuilder
                .WithEnemyIds(
                    monsterIds[0],
                    monsterIds[1],
                    monsterIds[2],
                    monsterIds[3],
                    monsterIds[4],
                    monsterIds[5])
                .WithIsScriptTriggeredCombat(false); // CombatEnterNormalFightCommand is used for
                                                     // normal combat only (player collide with monster)

            StartCombat();
        }

        public void Execute(CombatEnterBossFightCommand command)
        {
            _combatContextBuilder
                .WithEnemyIds(
                    command.Monster1Id,
                    command.Monster2Id,
                    command.Monster3Id,
                    command.Monster4Id,
                    command.Monster5Id,
                    command.Monster6Id)
                .WithIsScriptTriggeredCombat(true);
            StartCombat();
        }

        #if PAL3A
        public void Execute(CombatEnterBossFightUsingMusicCommand command)
        {
            _combatContextBuilder
                .WithEnemyIds(
                    command.Monster1Id,
                    command.Monster2Id,
                    command.Monster3Id,
                    command.Monster4Id,
                    command.Monster5Id,
                    command.Monster6Id)
                .WithIsScriptTriggeredCombat(true)
                .WithCombatMusicName(command.CombatMusicName);
            StartCombat();
        }

        public void Execute(CombatEnterBossFightUsingMusicWithSpecialActorCommand command)
        {
            _combatContextBuilder
                .WithEnemyIds(
                    command.Monster1Id,
                    command.Monster2Id,
                    command.Monster3Id,
                    command.Monster4Id,
                    command.Monster5Id,
                    command.Monster6Id)
                .WithIsScriptTriggeredCombat(true)
                .WithCombatMusicName(command.CombatMusicName);
            // TODO: NanGoongHuang enter fight using wolf state
            StartCombat();
        }
        #endif

        public void Execute(CombatSetUnbeatableCommand command)
        {
            _combatContextBuilder.WithUnbeatable(true);
        }

        public void Execute(CombatSetMaxRoundCommand command)
        {
            _combatContextBuilder.WithMaxRound(command.MaxRound);
        }

        public void Execute(CombatSetNoGameOverWhenLoseCommand command)
        {
            _combatContextBuilder.WithNoGameOverWhenLose(true);
        }

        public void Execute(ResetGameStateCommand command)
        {
            _combatContextBuilder.ResetContext();
        }

        // Player actor collides with combat NPC in maze
        public void Execute(CombatActorCollideWithPlayerActorNotification command)
        {
            Pal3.Instance.Execute(new ActorActivateCommand(command.CombatActorId, 0));

            if (_gameSettings.IsTurnBasedCombatEnabled)
            {
                Pal3.Instance.Execute(new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));

                GameScene currentScene = _sceneManager.GetCurrentScene();
                GameActor gameActor = currentScene.GetActor(command.CombatActorId);
                ITransform playerTransform = currentScene.GetActorGameEntity(command.PlayerActorId).Transform;
                ITransform enemyTransform = currentScene.GetActorGameEntity(command.CombatActorId).Transform;

                _combatContextBuilder.WithMeetType(CalculateMeetType(playerTransform, enemyTransform));

                Execute(new CombatEnterNormalFightCommand(
                    gameActor.Info.NumberOfMonsters,
                    gameActor.Info.MonsterIds[0],
                    gameActor.Info.MonsterIds[1],
                    gameActor.Info.MonsterIds[2]));
            }
            else // TODO: Remove once combat system is fully implemented
            {
                Pal3.Instance.Execute(new PlaySfxCommand("wd130", 1));
            }
        }

        private MeetType CalculateMeetType(ITransform playerTransform, ITransform enemyTransform)
        {
            Vector3 relativePosition = playerTransform.Position - enemyTransform.Position;

            // Normalize the vector for more accurate dot product
            relativePosition.Normalize();

            if (Vector3.Dot(playerTransform.Forward, -enemyTransform.Forward) > 0.7f)
            {
                return MeetType.RunningIntoEachOther;
            }
            else if (Vector3.Dot(playerTransform.Forward, -relativePosition) > 0.7f)
            {
                return MeetType.PlayerChasingEnemy;
            }
            else if (Vector3.Dot(enemyTransform.Forward, relativePosition) > 0.7f)
            {
                return MeetType.EnemyChasingPlayer;
            }

            return MeetType.RunningIntoEachOther;
        }
    }
}