// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.State
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
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.Contracts;
    using Core.DataReader.Scn;
    using Core.DataReader.Txt;
    using Core.GameBox;
    using Core.Utils;
    using Effect.PostProcessing;
    using GamePlay;
    using GameSystems.WorldMap;
    using GameSystems.Favor;
    using GameSystems.Inventory;
    using GameSystems.Team;
    using MetaData;
    using Scene;
    using Script;
    using UnityEngine;

    #if PAL3A
    using GameSystems.Task;
    #endif

    public enum SaveLevel
    {
        Minimal = 0,
        Full
    }

    public class SaveManager : IDisposable,
        ICommandExecutor<GameStateChangedNotification>,
        ICommandExecutor<GameSwitchToMainMenuCommand>
    {
        public const int AutoSaveSlotIndex = 0;
        public bool IsAutoSaveEnabled { get; set; } = false;

        private const string LEGACY_SAVE_FILE_NAME = "save.txt";
        private const string SAVE_FILE_FORMAT = "slot_{0}.txt";
        private const string SAVE_FOLDER_NAME = "Saves";
        private const float AUTO_SAVE_MIN_DURATION = 120f; // 2 minutes

        private readonly SceneManager _sceneManager;
        private readonly PlayerActorManager _playerActorManager;
        private readonly TeamManager _teamManager;
        private readonly InventoryManager _inventoryManager;
        private readonly SceneStateManager _sceneStateManager;
        private readonly WorldMapManager _worldMapManager;
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

            // Migrate old save file if exists
            // TODO: Remove this after a few versions
            {
                string legacySaveFilePath = Application.persistentDataPath +
                                            Path.DirectorySeparatorChar + LEGACY_SAVE_FILE_NAME;
                if (File.Exists(legacySaveFilePath))
                {
                    // Migrate old save file to slot 1
                    File.Move(legacySaveFilePath, GetSaveFilePath(1));
                }
            }

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        private string GetSaveFolderPath()
        {
            return Application.persistentDataPath + Path.DirectorySeparatorChar + SAVE_FOLDER_NAME;
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
                Debug.Log($"[{nameof(SaveManager)}] Game state saved to: {saveFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(SaveManager)}] Failed to save game state to file: {saveFilePath} with exception: {ex}");
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

            var playerActorMovementController = currentScene
                .GetActorGameObject((int) _playerActorManager.GetPlayerActor()).GetComponent<ActorMovementController>();
            Vector3 playerActorWorldPosition = playerActorMovementController.GetWorldPosition();
            Vector3 playerActorGameBoxPosition = GameBoxInterpreter
                .ToGameBoxPosition(playerActorMovementController.GetWorldPosition());

            var varsToSave = _scriptManager.GetGlobalVariables();
            if (saveLevel == SaveLevel.Minimal)
            {
                varsToSave = new Dictionary<int, int>()
                {
                    {ScriptConstants.MainStoryVariableName, varsToSave[ScriptConstants.MainStoryVariableName]}
                }; // Save main story var only
            }

            ScnSceneInfo currentSceneInfo = currentScene.GetSceneInfo();

            var commands = new List<ICommand>
            {
                new ResetGameStateCommand(), // Reset game state at first
            };

            // Save global variable(s)
            commands.AddRange(varsToSave.Select(var =>
                new ScriptVarSetValueCommand(var.Key, var.Value)));

            var currentPlayerActorId = (int)_playerActorManager.GetPlayerActor();

            // Save current playing script music (if any)
            var currentScriptMusic = _audioManager.GetCurrentScriptMusic();
            if (!string.IsNullOrEmpty(currentScriptMusic))
            {
                commands.Add(new PlayMusicCommand(currentScriptMusic, 0));
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
            var currentEffectMode = _postProcessManager.GetCurrentAppliedEffectMode();
            if (currentEffectMode != -1)
            {
                commands.Add(new EffectSetScreenEffectCommand(currentEffectMode));
            }

            // Save current camera state
            int currentCameraTransformOption = _cameraManager.GetCurrentAppliedDefaultTransformOption();
            Vector3 cameraCurrentRotationInEulerAngles = _cameraManager.GetMainCamera().transform.rotation.eulerAngles;
            commands.Add(new CameraSetInitialStateOnNextSceneLoadCommand(
                cameraCurrentRotationInEulerAngles, currentCameraTransformOption));

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
                    playerActorGameBoxPosition.y),
                new ActorSetFacingCommand(currentPlayerActorId,
                    (int)playerActorMovementController.gameObject.transform.rotation.eulerAngles.y)
            });

            var allActors = currentScene.GetAllActors();
            var allActorGameObjects = currentScene.GetAllActorGameObjects();

            // Save npc actor state
            foreach ((int actorId, GameObject actorGameObject)  in allActorGameObjects)
            {
                if (currentPlayerActorId == actorId) continue;
                SaveNpcActorState(commands, actorId, actorGameObject, allActors);
            }

            #if PAL3
            // Save LongKui state
            var longKuiCurrentMode = currentScene.GetActorGameObject((int) PlayerActorId.LongKui)
                .GetComponent<LongKuiController>()
                .GetCurrentMode();
            if (longKuiCurrentMode != 0)
            {
                commands.Add(new LongKuiSwitchModeCommand(longKuiCurrentMode));
            }

            // Save HuaYing state
            var huaYingGameObject = currentScene.GetActorGameObject((int) PlayerActorId.HuaYing);
            var huaYingCurrentMode = huaYingGameObject.GetComponent<HuaYingController>().GetCurrentMode();
            if (huaYingCurrentMode != 1)
            {
                commands.Add(new HuaYingSwitchBehaviourModeCommand(huaYingCurrentMode));
            }
            if (huaYingCurrentMode == 2 && huaYingGameObject.GetComponent<ActorController>().IsActive)
            {
                commands.Add(new ActorActivateCommand((int) PlayerActorId.HuaYing, 1));
                var huaYingMovementController = huaYingGameObject.GetComponent<ActorMovementController>();
                commands.Add(new ActorSetNavLayerCommand((int)PlayerActorId.HuaYing,
                    huaYingMovementController.GetCurrentLayerIndex()));
                Vector3 position = huaYingMovementController.GetWorldPosition();
                Vector3 gameBoxPosition = GameBoxInterpreter.ToGameBoxPosition(position);
                commands.Add(new ActorSetWorldPositionCommand((int)PlayerActorId.HuaYing, position.x, position.z));
                commands.Add(new ActorSetYPositionCommand((int)PlayerActorId.HuaYing, gameBoxPosition.y));
                commands.Add(new ActorSetFacingCommand((int)PlayerActorId.HuaYing,
                    (int)huaYingGameObject.transform.rotation.eulerAngles.y));
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
            GameObject actorGameObject,
            Dictionary<int, Actor> allActors)
        {
            var actorController = actorGameObject.GetComponent<ActorController>();

            // Save activation state if changed by the script
            if (!actorController.IsActive && allActors[actorId].Info.InitActive == 1)
            {
                commands.Add(new ActorActivateCommand(actorId, 0));
                return;
            }

            if (!actorController.IsActive) return; // Skip inactive actor

            Actor actor = actorController.GetActor();

            if (actor.Info.InitActive == 0)
            {
                commands.Add(new ActorActivateCommand(actorId, 1));
            }

            var actorMovementController = actorGameObject.GetComponent<ActorMovementController>();

            // Save position and rotation if not in initial state
            // Only save position and rotation if the actor behavior is None or Hold
            if (actor.Info.InitBehaviour is ActorBehaviourType.None or ActorBehaviourType.Hold)
            {
                if (actorMovementController.GetCurrentLayerIndex() != actor.Info.LayerIndex)
                {
                    commands.Add(new ActorSetNavLayerCommand(actorId,
                        actorMovementController.GetCurrentLayerIndex()));
                }

                var currentPosition = actorGameObject.transform.position;
                var currentGameBoxPosition = GameBoxInterpreter.ToGameBoxPosition(currentPosition);

                if (Mathf.Abs(currentGameBoxPosition.x - actor.Info.GameBoxXPosition) > 0.01f ||
                    Mathf.Abs(currentGameBoxPosition.z - actor.Info.GameBoxZPosition) > 0.01f)
                {
                    commands.Add(new ActorSetWorldPositionCommand(actorId,
                        currentPosition.x, currentPosition.z));
                }

                if (Mathf.Abs(currentGameBoxPosition.y - actor.Info.GameBoxYPosition) > 0.01f)
                {
                    commands.Add(new ActorSetYPositionCommand(actorId,
                        currentGameBoxPosition.y));
                }

                if (Quaternion.Euler(0, -actor.Info.FacingDirection, 0) !=
                    actorGameObject.transform.rotation)
                {
                    commands.Add(new ActorSetFacingCommand(actorId,
                        (int) actorGameObject.transform.rotation.eulerAngles.y));
                }
            }

            // Save script id if changed by the script
            if (actorController.IsScriptChanged())
            {
                commands.Add(new ActorSetScriptCommand(actorId,
                    (int) actor.Info.ScriptId));
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
                Time.realtimeSinceStartupAsDouble - _lastAutoSaveTime > AUTO_SAVE_MIN_DURATION)
            {
                IList<ICommand> gameStateCommands = ConvertCurrentGameStateToCommands(SaveLevel.Full);

                bool success = SaveGameStateToSlot(AutoSaveSlotIndex, gameStateCommands);
                if (success)
                {
                    _lastAutoSaveTime = Time.realtimeSinceStartupAsDouble;
                }

                Debug.LogWarning($"[{nameof(SaveManager)}] Game state auto-saved to slot {AutoSaveSlotIndex}.");
            }
        }

        public void Execute(GameSwitchToMainMenuCommand command)
        {
            IsAutoSaveEnabled = false;
        }
    }
}