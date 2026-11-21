// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.GameSystems.Battle
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
    public sealed class BattleCoordinator : IDisposable,
        ICommandExecutor<BattleActorCollideWithPlayerActorNotification>,
        ICommandExecutor<CombatEnterNormalFightCommand>,
        ICommandExecutor<CombatEnterBossFightCommand>,
#if PAL3A
        ICommandExecutor<CombatEnterBossFightUsingMusicCommand>,
        ICommandExecutor<CombatPrepareNanGongHuangTransformCommand>,
        ICommandExecutor<CombatPrepareWangPengXuTransformCommand>,
        ICommandExecutor<NanGongHuangEnableCombatAbilitiesCommand>,
        ICommandExecutor<WangPengXuEnableCombatTransformCommand>,
        ICommandExecutor<CombatPreserveDamageForLiaoRiCommand>,
        ICommandExecutor<Pal3ARestoreCombatScriptStateCommand>,
#endif
        ICommandExecutor<CombatSetUnbeatableCommand>,
        ICommandExecutor<CombatSetMaxRoundCommand>,
        ICommandExecutor<CombatSetNoGameOverWhenLoseCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private const string COMBAT_SCN_FILE_NAME = "combatScn.txt";

#if PAL3A
        public Core.Contract.Pal3ACombatScriptState ScriptState { get; } = new();

#endif
        private readonly GameSettings _gameSettings;
        private readonly BattleManager _battleManager;
        private readonly PlayerActorManager _playerActorManager;
        private readonly AudioManager _audioManager;
        private readonly SceneManager _sceneManager;
        private readonly GameStateManager _gameStateManager;

        private readonly CombatScnFile _battleScnFile;
        private readonly BattleContextBuilder _battleContextBuilder = new();

        public BattleCoordinator(GameResourceProvider resourceProvider,
            GameSettings gameSettings,
            BattleManager combatManager,
            PlayerActorManager playerActorManager,
            AudioManager audioManager,
            SceneManager sceneManager,
            GameStateManager gameStateManager)
        {
            Requires.IsNotNull(resourceProvider, nameof(resourceProvider));
            _gameSettings = Requires.IsNotNull(gameSettings, nameof(gameSettings));
            _battleManager = Requires.IsNotNull(combatManager, nameof(combatManager));
            _playerActorManager = Requires.IsNotNull(playerActorManager, nameof(playerActorManager));
            _audioManager = Requires.IsNotNull(audioManager, nameof(audioManager));
            _sceneManager = Requires.IsNotNull(sceneManager, nameof(sceneManager));
            _gameStateManager = Requires.IsNotNull(gameStateManager, nameof(gameStateManager));

            _battleScnFile = resourceProvider.GetGameResourceFile<CombatScnFile>(
                FileConstants.DataScriptFolderVirtualPath + COMBAT_SCN_FILE_NAME);

            _battleManager.OnBattleFinished += OnBattleFinished;
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            _battleManager.OnBattleFinished -= OnBattleFinished;
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        private void StartBattle()
        {
            if (!_gameSettings.IsTurnBasedCombatEnabled)
            {
                // A skipped encounter must not leave its one-shot flags on an unrelated later fight.
#if PAL3A
                ScriptState.BeginBattle(Array.IndexOf(_battleContextBuilder.CurrentContext.EnemyIds, 722u) >= 0);
#endif
                _battleContextBuilder.ResetContext();
                return;
            }

            GameState previousState = _gameStateManager.GetCurrentState();
#if PAL3A
            int flags = ScriptState.Flags, lostHp = ScriptState.LostHp, lostMp = ScriptState.LostMp;
#endif
            try
            {
                GameScene currentScene = _sceneManager.GetCurrentScene();

                // Figure out which combat scene to use
                string combatSceneName = GetBattleSceneName(currentScene);
                _battleContextBuilder.WithBattleSceneName(combatSceneName);

                // Figure out element property of the combat scene
                if (_battleScnFile.CombatSceneElementTypeInfo.TryGetValue(combatSceneName,
                        out ElementType combatSceneElementType))
                {
                    _battleContextBuilder.WithBattleSceneElementType(combatSceneElementType);
                }

                // Figure out which combat music to play
                if (string.IsNullOrEmpty(_battleContextBuilder.CurrentContext.BattleMusicName))
                {
                    if (MusicConstants.CombatMusicInfo.TryGetValue(
                            currentScene.GetSceneInfo().CityName.ToLower(),
                            out string combatMusic))
                    {
                        // Set music only if it's not set yet or it's not a script triggered combat
                        if (string.IsNullOrEmpty(_audioManager.GetCurrentScriptMusic()) ||
                            !_battleContextBuilder.CurrentContext.IsScriptTriggeredBattle)
                        {
                            _battleContextBuilder.WithBattleMusicName(combatMusic);
                        }
                    }
                }

                // Check current state and add script waiter to pause script execution
                // if we are in cutscene state.
                if (_gameStateManager.GetCurrentState() == GameState.Cutscene)
                {
                    WaitUntilCanceled combatWaiter = new();
                    Pal3.Instance.Execute(new ScriptRunnerAddWaiterRequest(combatWaiter));
                    _battleContextBuilder.WithScriptWaiter(combatWaiter);
                }

                BattleContext combatContext = _battleContextBuilder.Build();

#if PAL3A
                var options = ScriptState.BeginBattle(Array.IndexOf(combatContext.EnemyIds, 722u) >= 0);
                combatContext.NanGongHuangAbilitiesEnabled = ScriptState.NanGongHuangAbilitiesEnabled;
                combatContext.WangPengXuTransformEnabled = ScriptState.WangPengXuTransformEnabled;
                combatContext.NanGongHuangStartsTransformed = options.wolf;
                combatContext.WangPengXuStartsTransformed = options.pengXu;
                combatContext.LiaoRiHpBonus = options.hpBonus;
                combatContext.LiaoRiMpBonus = options.mpBonus;
#endif

                // Start combat
                EngineLogger.LogWarning($"Starting combat with context: {combatContext}");
                _gameStateManager.TryGoToState(GameState.Combat);
                _battleManager.EnterBattle(combatContext);
            }
            catch
            {
#if PAL3A
                ScriptState.Restore(flags, lostHp, lostMp);
#endif
                _battleContextBuilder.CurrentContext.ScriptWaiter?.CancelWait();
                _battleContextBuilder.ResetContext();
                if (!_gameStateManager.TryGoToState(previousState))
                    _gameStateManager.TryGoToState(GameState.Cutscene);
                throw;
            }
        }

        private void OnBattleFinished(object sender, BattleResult combatResult)
        {
            _battleManager.ExitBattle();
            combatResult.BattleContext.ScriptWaiter?.CancelWait();
            _battleContextBuilder.ResetContext();

            if (combatResult.IsPlayerWin)
            {
                _gameStateManager.GoToPreviousState();
            }
            else
            {
                Pal3.Instance.Execute(new GameSwitchToMainMenuCommand());
            }
        }

        private string GetBattleSceneName(GameScene currentScene)
        {
            ScnSceneInfo sceneInfo = currentScene.GetSceneInfo();

            if (!_battleScnFile.CombatSceneMapInfo.TryGetValue($"{sceneInfo.CityName}_{sceneInfo.SceneName}",
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

            _battleContextBuilder
                .WithEnemyIds(
                    monsterIds[0],
                    monsterIds[1],
                    monsterIds[2],
                    monsterIds[3],
                    monsterIds[4],
                    monsterIds[5])
                .WithIsScriptTriggeredBattle(false); // CombatEnterNormalFightCommand is used for
                                                     // normal combat only (player collide with monster)

            StartBattle();
        }

        public void Execute(CombatEnterBossFightCommand command)
        {
            _battleContextBuilder
                .WithEnemyIds(
                    command.Monster1Id,
                    command.Monster2Id,
                    command.Monster3Id,
                    command.Monster4Id,
                    command.Monster5Id,
                    command.Monster6Id)
                .WithIsScriptTriggeredBattle(true);
            StartBattle();
        }

#if PAL3A
        public void Execute(CombatEnterBossFightUsingMusicCommand command)
        {
            _battleContextBuilder
                .WithEnemyIds(
                    command.Monster1Id,
                    command.Monster2Id,
                    command.Monster3Id,
                    command.Monster4Id,
                    command.Monster5Id,
                    command.Monster6Id)
                .WithIsScriptTriggeredBattle(true)
                .WithBattleMusicName(command.CombatMusicName);
            StartBattle();
        }

        public void Execute(CombatPrepareNanGongHuangTransformCommand command) => ScriptState.NextNanGongHuangTransform = true;

        public void Execute(CombatPrepareWangPengXuTransformCommand command) => ScriptState.NextWangPengXuTransform = true;

        public void Execute(NanGongHuangEnableCombatAbilitiesCommand command) => ScriptState.NanGongHuangAbilitiesEnabled = command.Enable > 0;

        public void Execute(WangPengXuEnableCombatTransformCommand command) => ScriptState.WangPengXuTransformEnabled = command.Enabled > 0;

        public void Execute(CombatPreserveDamageForLiaoRiCommand command) => ScriptState.PreserveLosses = true;

        public void Execute(Pal3ARestoreCombatScriptStateCommand command) => ScriptState.Restore(command.Flags, command.LostHp, command.LostMp);
#endif

        public void Execute(CombatSetUnbeatableCommand command)
        {
            _battleContextBuilder.WithUnbeatable(true);
        }

        public void Execute(CombatSetMaxRoundCommand command)
        {
            _battleContextBuilder.WithMaxRound(command.MaxRound);
        }

        public void Execute(CombatSetNoGameOverWhenLoseCommand command)
        {
            _battleContextBuilder.WithNoGameOverWhenLose(true);
        }

        public void Execute(ResetGameStateCommand command)
        {
            _battleContextBuilder.ResetContext();
#if PAL3A
            ScriptState.Reset();
#endif
        }

        // Player actor collides with combat NPC in maze
        public void Execute(BattleActorCollideWithPlayerActorNotification command)
        {
            Pal3.Instance.Execute(new ActorActivateCommand(command.CombatActorId, 0));

            if (_gameSettings.IsTurnBasedCombatEnabled)
            {
                Pal3.Instance.Execute(new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));

                GameScene currentScene = _sceneManager.GetCurrentScene();
                GameActor gameActor = currentScene.GetActor(command.CombatActorId);
                ITransform playerTransform = currentScene.GetActorGameEntity(command.PlayerActorId).Transform;
                ITransform enemyTransform = currentScene.GetActorGameEntity(command.CombatActorId).Transform;

                _battleContextBuilder.WithMeetType(CalculateMeetType(playerTransform, enemyTransform));

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
