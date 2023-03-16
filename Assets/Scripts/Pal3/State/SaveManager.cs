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
    using Audio;
    using Camera;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Effect;
    using Effect.PostProcessing;
    using Feature;
    using MetaData;
    using Player;
    using Scene;
    using Script;
    using UI;
    using UnityEngine;
    using Path = System.IO.Path;

    public enum SaveLevel
    {
        Minimal = 0,
        Full
    }

    public class SaveManager
    {
        private const string SAVE_FILE_NAME = "save.txt";

        private readonly SceneManager _sceneManager;
        private readonly PlayerManager _playerManager;
        private readonly TeamManager _teamManager;
        private readonly InventoryManager _inventoryManager;
        private readonly SceneStateManager _sceneStateManager;
        private readonly BigMapManager _bigMapManager;
        private readonly ScriptManager _scriptManager;
        private readonly FavorManager _favorManager;
        private readonly CameraManager _cameraManager;
        private readonly AudioManager _audioManager;
        private readonly PostProcessManager _postProcessManager;

        public SaveManager(SceneManager sceneManager,
            PlayerManager playerManager,
            TeamManager teamManager,
            InventoryManager inventoryManager,
            SceneStateManager sceneStateManager,
            BigMapManager bigMapManager,
            ScriptManager scriptManager,
            FavorManager favorManager,
            CameraManager cameraManager,
            AudioManager audioManager,
            PostProcessManager postProcessManager)
        {
            _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
            _playerManager = playerManager ?? throw new ArgumentNullException(nameof(playerManager));
            _teamManager = teamManager ?? throw new ArgumentNullException(nameof(teamManager));
            _inventoryManager = inventoryManager ?? throw new ArgumentNullException(nameof(inventoryManager));
            _sceneStateManager = sceneStateManager ?? throw new ArgumentNullException(nameof(sceneStateManager));
            _bigMapManager = bigMapManager != null ? bigMapManager : throw new ArgumentNullException(nameof(bigMapManager));
            _scriptManager = scriptManager ?? throw new ArgumentNullException(nameof(scriptManager));
            _favorManager = favorManager ?? throw new ArgumentNullException(nameof(favorManager));
            _cameraManager = cameraManager != null ? cameraManager : throw new ArgumentNullException(nameof(cameraManager));
            _audioManager = audioManager != null ? audioManager : throw new ArgumentNullException(nameof(audioManager));
            _postProcessManager = postProcessManager != null ? postProcessManager : throw new ArgumentNullException(nameof(postProcessManager));
        }

        private string GetSaveFilePath()
        {
            return Application.persistentDataPath +
                   Path.DirectorySeparatorChar + SAVE_FILE_NAME;
        }

        public bool SaveFileExists()
        {
            return File.Exists(GetSaveFilePath());
        }

        public bool SaveGameStateToFile()
        {
            string saveFilePath = GetSaveFilePath();
            try
            {
                var commands = ConvertCurrentGameStateToCommands(SaveLevel.Full);
                File.WriteAllText(saveFilePath, string.Join('\n', commands.Select(CommandExtensions.ToString).ToList()));
                Debug.Log($"Game state saved to: {saveFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save game state to file: {saveFilePath} with exception: {ex}");
                return false;
            }
        }

        public string LoadFromSaveFile()
        {
            return !SaveFileExists() ? null : File.ReadAllText(GetSaveFilePath());
        }

        public List<ICommand> ConvertCurrentGameStateToCommands(SaveLevel saveLevel)
        {
            if (_sceneManager.GetCurrentScene() is not { } currentScene) return null;

            var playerActorMovementController = currentScene
                .GetActorGameObject((int) _playerManager.GetPlayerActor()).GetComponent<ActorMovementController>();
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

            var currentPlayerActorId = (int)_playerManager.GetPlayerActor();

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

            // Save big map region activation state
            commands.AddRange(_bigMapManager.GetRegionEnablementInfo()
                .Select(regionEnablement => new BigMapEnableRegionCommand(regionEnablement.Key, regionEnablement.Value)));

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

            // Save actor activation state changed by the script
            // + actor position changed by the script
            // + actor rotation changed by the script
            // + actor layer changed by the script
            // + actor script id changed by the script
            foreach ((int actorId, GameObject actorGameObject)  in allActorGameObjects)
            {
                if (currentPlayerActorId == actorId) continue;

                var actorController = actorGameObject.GetComponent<ActorController>();
                var actorMovementController = actorGameObject.GetComponent<ActorMovementController>();

                if (!actorController.IsActive && allActors[actorId].Info.InitActive == 1)
                {
                    commands.Add(new ActorActivateCommand(actorId, 0));
                }
                else if (actorController.IsActive)
                {
                    Actor actor = actorController.GetActor();

                    if (actor.Info.InitActive == 0)
                    {
                        commands.Add(new ActorActivateCommand(actorId, 1));
                    }

                    // Save position and rotation if not in initial state
                    // Only save position and rotation if the actor behavior is None or Hold
                    if (actor.Info.InitBehaviour is ScnActorBehaviour.None or ScnActorBehaviour.Hold)
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
                            (int)actor.Info.ScriptId));
                    }
                }
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
            #endif

            // Good to have
            commands.Add(new CameraFadeInCommand());
            return commands;
        }
    }
}