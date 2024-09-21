﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.State
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Actor;
    using Actor.Controllers;
    using Audio;
    using Camera;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Core.Primitives;
    using Core.Utilities;
    using Effect.PostProcessing;
    using Engine.Core.Abstraction;
    using Engine.Extensions;
    using Engine.Logging;
    using Engine.Services;
    using GamePlay;
    using GameSystems.WorldMap;
    using GameSystems.Favor;
    using GameSystems.Inventory;
    using GameSystems.Team;
    using Scene;
    using Script;

    #if PAL3A
    using GameSystems.Task;
    #endif

    using Quaternion = UnityEngine.Quaternion;
    using Vector3 = UnityEngine.Vector3;

    public enum SaveLevel
    {
        Minimal = 0,
        Full
    }

    public sealed class SaveManager : IDisposable,
        ICommandExecutor<GameStateChangedNotification>,
        ICommandExecutor<GameSwitchToMainMenuCommand>
    {
        public const int AutoSaveSlotIndex = 0;
        public bool IsAutoSaveEnabled { get; set; } = false;

        private const string SAVE_FILE_FORMAT = "slot_{0}_v1.txt";
        private const string SAVE_FOLDER_NAME = "Saves";
        private const float AUTO_SAVE_MIN_DURATION = 120f; // 2 minutes

        private readonly SceneManager _sceneManager;
        private readonly PlayerActorManager _playerActorManager;
        private readonly TeamManager _teamManager;
        private readonly InventoryManager _inventoryManager;
        private readonly SceneStateManager _sceneStateManager;
        private readonly WorldMapManager _worldMapManager;
        private readonly IUserVariableStore<ushort, int> _userVariableStore;
        private readonly ScriptManager _scriptManager;
        private readonly FavorManager _favorManager;
        #if PAL3A
        private readonly TaskManager _taskManager;
        #endif
        private readonly CameraManager _cameraManager;
        private readonly AudioManager _audioManager;
        private readonly PostProcessManager _postProcessManager;

        private double _lastAutoSaveTime = -AUTO_SAVE_MIN_DURATION;

        public SaveManager(SceneManager sceneManager,
            PlayerActorManager playerActorManager,
            TeamManager teamManager,
            InventoryManager inventoryManager,
            SceneStateManager sceneStateManager,
            WorldMapManager worldMapManager,
            IUserVariableStore<ushort, int> userVariableStore,
            ScriptManager scriptManager,
            FavorManager favorManager,
            #if PAL3A
            TaskManager taskManager,
            #endif
            CameraManager cameraManager,
            AudioManager audioManager,
            PostProcessManager postProcessManager)
        {
            _sceneManager = Requires.IsNotNull(sceneManager, nameof(sceneManager));
            _playerActorManager = Requires.IsNotNull(playerActorManager, nameof(playerActorManager));
            _teamManager = Requires.IsNotNull(teamManager, nameof(teamManager));
            _inventoryManager = Requires.IsNotNull(inventoryManager, nameof(inventoryManager));
            _sceneStateManager = Requires.IsNotNull(sceneStateManager, nameof(sceneStateManager));
            _worldMapManager = Requires.IsNotNull(worldMapManager, nameof(worldMapManager));
            _userVariableStore = Requires.IsNotNull(userVariableStore, nameof(userVariableStore));
            _scriptManager = Requires.IsNotNull(scriptManager, nameof(scriptManager));
            _favorManager = Requires.IsNotNull(favorManager, nameof(favorManager));
            #if PAL3A
            _taskManager = Requires.IsNotNull(taskManager, nameof(taskManager));
            #endif
            _cameraManager = Requires.IsNotNull(cameraManager, nameof(cameraManager));
            _audioManager = Requires.IsNotNull(audioManager, nameof(audioManager));
            _postProcessManager = Requires.IsNotNull(postProcessManager, nameof(postProcessManager));

            string saveFolder = GetSaveFolderPath();
            if (!Directory.Exists(saveFolder))
            {
                Directory.CreateDirectory(saveFolder);
            }

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        private string GetSaveFolderPath()
        {
            return UnityEngine.Application.persistentDataPath + Path.DirectorySeparatorChar + SAVE_FOLDER_NAME;
        }

        private string GetSaveFilePath(int slotIndex)
        {
            return GetSaveFolderPath() + Path.DirectorySeparatorChar + string.Format(SAVE_FILE_FORMAT, slotIndex);
        }

        public bool SaveSlotExists(int slotIndex)
        {
            return File.Exists(GetSaveFilePath(slotIndex));
        }

        public DateTime GetSaveSlotLastWriteTime(int slotIndex)
        {
            return File.GetLastWriteTime(GetSaveFilePath(slotIndex));
        }

        public bool SaveGameStateToSlot(int slotIndex, IList<ICommand> stateCommands)
        {
            string saveFilePath = GetSaveFilePath(slotIndex);
            try
            {
                File.WriteAllText(saveFilePath, string.Join('\n', stateCommands.Select(CommandExtensions.ToString).ToList()));
                EngineLogger.Log($"Game state saved to: {saveFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                EngineLogger.LogException(ex);
                return false;
            }
        }

        public string LoadFromSaveSlot(int slotIndex)
        {
            return SaveSlotExists(slotIndex) ? File.ReadAllText(GetSaveFilePath(slotIndex)) : null;
        }

        public List<ICommand> ConvertCurrentGameStateToCommands(SaveLevel saveLevel)
        {
            if (_sceneManager.GetCurrentScene() is not { } currentScene) return null;

            ActorMovementController playerActorMovementController = currentScene
                .GetActorGameEntity(_playerActorManager.GetPlayerActorId()).GetComponent<ActorMovementController>();
            Vector3 playerActorWorldPosition = playerActorMovementController.GetWorldPosition();
            GameBoxVector3 playerActorGameBoxPosition = playerActorMovementController.GetWorldPosition().ToGameBoxPosition();

            IDictionary<ushort, int> variables;
            if (saveLevel == SaveLevel.Minimal)
            {
                variables = new Dictionary<ushort, int>();
                // Save main story var only
                variables[ScriptConstants.MainStoryVariableId] =
                    _userVariableStore.Get(ScriptConstants.MainStoryVariableId);
            }
            else
            {
                variables = new Dictionary<ushort, int>(_userVariableStore);
            }

            ScnSceneInfo currentSceneInfo = currentScene.GetSceneInfo();

            List<ICommand> commands = new ()
            {
                new ResetGameStateCommand(), // Reset game state at first
            };

            // Save global variable(s)
            commands.AddRange(variables.Select(var =>
                new ScriptVarSetValueCommand(var.Key, var.Value)));

            int currentPlayerActorId = _playerActorManager.GetPlayerActorId();

            // Save current playing script music (if any)
            string currentScriptMusic = _audioManager.GetCurrentScriptMusic();
            if (!string.IsNullOrEmpty(currentScriptMusic))
            {
                commands.Add(new PlayScriptMusicCommand(currentScriptMusic, 0));
            }

            // Save team state
            commands.AddRange(_teamManager.GetActorsInTeam()
                .Select(actorId => new TeamAddOrRemoveActorCommand((int) actorId, 1)));

            // Save favor info
            commands.AddRange(_favorManager.GetAllActorFavorInfo()
                .Select(favorInfo => new FavorAddCommand(favorInfo.Key, favorInfo.Value)));

            // Save world map region activation state
            commands.AddRange(_worldMapManager.GetRegionEnablementInfo()
                .Select(regionEnablement => new WorldMapEnableRegionCommand(regionEnablement.Key, regionEnablement.Value)));

            // Save inventory state
            commands.Add(new InventoryAddMoneyCommand(_inventoryManager.GetTotalMoney()));
            commands.AddRange(_inventoryManager.GetAllItems()
                .Select(item => new InventoryAddItemCommand(item.Key, item.Value)));

            // Save scene object state
            foreach (var sceneObjectStateOverride in _sceneStateManager.GetSceneObjectStateOverrides())
            {
                commands.AddRange(sceneObjectStateOverride.Value.ToCommands(sceneObjectStateOverride.Key));
            }

            // Save current applied screen effect state
            int currentEffectMode = _postProcessManager.GetCurrentAppliedEffectMode();
            if (currentEffectMode != -1)
            {
                commands.Add(new EffectSetScreenEffectCommand(currentEffectMode));
            }

            // Save current camera state
            int currentCameraTransformOption = _cameraManager.GetCurrentAppliedDefaultTransformOption();
            Vector3 cameraCurrentRotationInEulerAngles = _cameraManager.GetCameraTransform().EulerAngles;
            commands.Add(new CameraSetInitialStateOnNextSceneLoadCommand(
                cameraCurrentRotationInEulerAngles.x,
                cameraCurrentRotationInEulerAngles.y,
                cameraCurrentRotationInEulerAngles.z,
                currentCameraTransformOption));

            // Save current scene info and player actor state
            commands.AddRange(new List<ICommand>()
            {
                new SceneLoadCommand(currentSceneInfo.CityName, currentSceneInfo.SceneName),
                new ActorActivateCommand(currentPlayerActorId, 1),
                new ActorEnablePlayerControlCommand(currentPlayerActorId),
                new PlayerEnableInputCommand(1),
                new ActorSetNavLayerCommand(currentPlayerActorId,
                    playerActorMovementController.GetCurrentLayerIndex()),
                new ActorSetWorldPositionCommand(currentPlayerActorId,
                    playerActorWorldPosition.x, playerActorWorldPosition.z),
                new ActorSetYPositionCommand(currentPlayerActorId,
                    playerActorGameBoxPosition.Y),
                new ActorSetFacingCommand(currentPlayerActorId,
                    (int)playerActorMovementController.GameEntity.Transform.EulerAngles.y)
            });

            Dictionary<int, GameActor> allActors = currentScene.GetAllActors();
            Dictionary<int, IGameEntity> allActorGameEntities = currentScene.GetAllActorGameEntities();

            // Save npc actor state
            foreach ((int actorId, IGameEntity actorGameEntity) in allActorGameEntities)
            {
                if (currentPlayerActorId == actorId) continue;
                SaveNpcActorState(commands, actorId, actorGameEntity, allActors);
            }

            #if PAL3
            // Save LongKui state
            int longKuiCurrentMode = currentScene.GetActorGameEntity((int) PlayerActorId.LongKui)
                .GetComponent<LongKuiController>()
                .GetCurrentMode();
            if (longKuiCurrentMode != 0)
            {
                commands.Add(new LongKuiSwitchModeCommand(longKuiCurrentMode));
            }

            // Save HuaYing state
            IGameEntity huaYingGameEntity = currentScene.GetActorGameEntity((int) PlayerActorId.HuaYing);
            int huaYingCurrentMode = huaYingGameEntity.GetComponent<HuaYingController>().GetCurrentMode();
            if (huaYingCurrentMode != 1)
            {
                commands.Add(new HuaYingSwitchBehaviourModeCommand(huaYingCurrentMode));
            }
            if (huaYingCurrentMode == 2 && huaYingGameEntity.GetComponent<ActorController>().IsActive)
            {
                commands.Add(new ActorActivateCommand((int) PlayerActorId.HuaYing, 1));
                ActorMovementController huaYingMovementController = huaYingGameEntity.GetComponent<ActorMovementController>();
                commands.Add(new ActorSetNavLayerCommand((int)PlayerActorId.HuaYing,
                    huaYingMovementController.GetCurrentLayerIndex()));
                Vector3 position = huaYingMovementController.GetWorldPosition();
                GameBoxVector3 gameBoxPosition = position.ToGameBoxPosition();
                commands.Add(new ActorSetWorldPositionCommand((int)PlayerActorId.HuaYing, position.x, position.z));
                commands.Add(new ActorSetYPositionCommand((int)PlayerActorId.HuaYing, gameBoxPosition.Y));
                commands.Add(new ActorSetFacingCommand((int)PlayerActorId.HuaYing,
                    (int)huaYingGameEntity.Transform.EulerAngles.y));
            }
            #elif PAL3A
            // Save Task state
            commands.AddRange(_taskManager.GetOpenedTasks().
                Select(openedTask => new TaskOpenCommand(openedTask.Id)));
            commands.AddRange(_taskManager.GetCompletedTasks().
                Select(completedTask => new TaskCompleteCommand(completedTask.Id)));
            #endif

            // Good to have
            commands.Add(new CameraFadeInCommand());

            return commands;
        }

        // Save npc actor activation state changed by the script
        // + actor position changed by the script
        // + actor rotation changed by the script
        // + actor layer changed by the script
        // + actor script id changed by the script
        private static void SaveNpcActorState(List<ICommand> commands,
            int actorId,
            IGameEntity actorGameEntity,
            Dictionary<int, GameActor> allActors)
        {
            ActorController actorController = actorGameEntity.GetComponent<ActorController>();

            // Save activation state if changed by the script
            if (!actorController.IsActive && allActors[actorId].Info.InitActive == 1)
            {
                commands.Add(new ActorActivateCommand(actorId, 0));
                return;
            }

            if (!actorController.IsActive) return; // Skip inactive actor

            GameActor actor = actorController.GetActor();

            if (actor.Info.InitActive == 0)
            {
                commands.Add(new ActorActivateCommand(actorId, 1));
            }

            ActorMovementController actorMovementController = actorGameEntity.GetComponent<ActorMovementController>();

            // Save position and rotation if not in initial state
            // Only save position and rotation if the actor behavior is None or Hold
            if (actor.Info.InitBehaviour is ActorBehaviourType.None or ActorBehaviourType.Hold)
            {
                if (actorMovementController.GetCurrentLayerIndex() != actor.Info.LayerIndex)
                {
                    commands.Add(new ActorSetNavLayerCommand(actorId,
                        actorMovementController.GetCurrentLayerIndex()));
                }

                Vector3 currentPosition = actorGameEntity.Transform.Position;
                GameBoxVector3 currentGameBoxPosition = currentPosition.ToGameBoxPosition();

                if (MathF.Abs(currentGameBoxPosition.X - actor.Info.GameBoxXPosition) > 0.01f ||
                    MathF.Abs(currentGameBoxPosition.Z - actor.Info.GameBoxZPosition) > 0.01f)
                {
                    commands.Add(new ActorSetWorldPositionCommand(actorId,
                        currentPosition.x, currentPosition.z));
                }

                if (MathF.Abs(currentGameBoxPosition.Y - actor.Info.GameBoxYPosition) > 0.01f)
                {
                    commands.Add(new ActorSetYPositionCommand(actorId,
                        currentGameBoxPosition.Y));
                }

                if (Quaternion.Euler(0, -actor.Info.FacingDirection, 0) !=
                    actorGameEntity.Transform.Rotation)
                {
                    commands.Add(new ActorSetFacingCommand(actorId,
                        (int) actorGameEntity.Transform.EulerAngles.y));
                }
            }

            // Save script id if changed by the script
            if (actor.IsScriptIdChanged())
            {
                commands.Add(new ActorSetScriptCommand(actorId,
                    (int)actor.GetScriptId()));
            }
        }

        public void Execute(GameStateChangedNotification command)
        {
            if (!IsAutoSaveEnabled) return;

            // Auto save game state in auto-save slot when entering gameplay state
            // and no script is running
            if (command.NewState == GameState.Gameplay &&
                _scriptManager.GetNumberOfRunningScripts() == 0 &&
                _playerActorManager.IsPlayerInputEnabled() &&
                _playerActorManager.IsPlayerActorControlEnabled() &&
                GameTimeProvider.Instance.RealTimeSinceStartup - _lastAutoSaveTime > AUTO_SAVE_MIN_DURATION)
            {
                IList<ICommand> gameStateCommands = ConvertCurrentGameStateToCommands(SaveLevel.Full);

                if (SaveGameStateToSlot(AutoSaveSlotIndex, gameStateCommands))
                {
                    _lastAutoSaveTime = GameTimeProvider.Instance.RealTimeSinceStartup;
                    EngineLogger.LogWarning($"Game state auto-saved to slot {AutoSaveSlotIndex}");
                }
            }
        }

        public void Execute(GameSwitchToMainMenuCommand command)
        {
            IsAutoSaveEnabled = false;
        }
    }
}