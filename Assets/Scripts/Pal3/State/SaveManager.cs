// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
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

        private const string SAVE_FILE_NAME = "save.txt";
        
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
                .GetActorGameObject((byte) _playerManager.GetPlayerActor()).GetComponent<ActorMovementController>();
            Vector2Int playerActorTilePosition = playerActorMovementController.GetTilePosition();

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
            
            // Save scene object activation state
            commands.AddRange(_sceneStateManager.GetSceneObjectActivationStates()
                .Select(state => new SceneChangeGlobalObjectActivationStateCommand(state.Key, state.Value ? 1 : 0)));
            
            // Save current applied screen effect state
            var currentEffectMode = _postProcessManager.GetCurrentAppliedEffectMode();
            if (currentEffectMode != -1)
            {
                commands.Add(new EffectSetScreenEffectCommand(currentEffectMode));   
            }
            
            // Save current scene info and player actor state
            commands.AddRange(new List<ICommand>()
            {
                new SceneLoadCommand(currentSceneInfo.CityName, currentSceneInfo.Name),
                new ActorActivateCommand(currentPlayerActorId, 1),
                new ActorEnablePlayerControlCommand(currentPlayerActorId),
                new PlayerEnableInputCommand(1),
                new ActorSetNavLayerCommand(currentPlayerActorId,
                    playerActorMovementController.GetCurrentLayerIndex()),
                new ActorSetTilePositionCommand(currentPlayerActorId,
                    playerActorTilePosition.x, playerActorTilePosition.y),
                new ActorRotateFacingCommand(currentPlayerActorId,
                    -(int)playerActorMovementController.gameObject.transform.rotation.eulerAngles.y)
            });

            if (saveLevel == SaveLevel.Full)
            {
                // Save scene object activation state changed by the script
                var allSceneObjects = currentScene.GetAllSceneObjects();
                var allActivatedSceneObjects = currentScene.GetAllActivatedSceneObjects();
                commands.AddRange(allActivatedSceneObjects
                    .Where(_ => allSceneObjects[_.Key].Info.Active == 0)
                    .Select(_ => new SceneActivateObjectCommand(_.Key, 1)));
                commands.AddRange(allSceneObjects
                    .Where(_ => allSceneObjects[_.Key].Info.Active == 1)
                    .Where(_ => !allActivatedSceneObjects.Keys.Contains(_.Key))
                    .Select(_ => new SceneActivateObjectCommand(_.Key, 0)));

                // Save actor activation state changed by the script
                var playerActorIds = Enum.GetValues(typeof(PlayerActorId)).Cast<int>().ToList();
                var allActors = currentScene.GetAllActors().Where(_ => !playerActorIds.Contains(_.Key)).ToArray();
                var allActorGameObjects = currentScene.GetAllActorGameObjects();
                commands.AddRange(allActors
                    .Where(_ => _.Value.Info.InitActive == 0)
                    .Where(_ => allActorGameObjects.ContainsKey(_.Key) &&
                                allActorGameObjects[_.Key].GetComponent<ActorController>().IsActive)
                    .Select(_ => new ActorActivateCommand(_.Key, 1)));
                commands.AddRange(allActors
                    .Where(_ => _.Value.Info.InitActive == 1)
                    .Where(_ => allActorGameObjects.ContainsKey(_.Key) &&
                                !allActorGameObjects[_.Key].GetComponent<ActorController>().IsActive)
                    .Select(_ => new ActorActivateCommand(_.Key, 0)));
            }

            #if PAL3
            // Save LongKui state
            var longKuiCurrentMode = currentScene.GetActorGameObject((byte) PlayerActorId.LongKui)
                .GetComponent<LongKuiController>()
                .GetCurrentMode();
            if (longKuiCurrentMode != 0)
            {
                commands.Add(new LongKuiSwitchModeCommand(longKuiCurrentMode));
            }

            // Save HuaYing state
            var huaYingGameObject = currentScene.GetActorGameObject((byte) PlayerActorId.HuaYing);
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
                Vector2Int tilePosition = huaYingMovementController.GetTilePosition();
                commands.Add(new ActorSetTilePositionCommand((int)PlayerActorId.HuaYing,
                    tilePosition.x,
                    tilePosition.y));
                var rotationInDegrees = (int)huaYingGameObject.transform.rotation.eulerAngles.y;
                commands.Add(new ActorRotateFacingCommand((int)PlayerActorId.HuaYing, -rotationInDegrees));
            }
            #endif

            // Save current applied camera settings
            var defaultCameraTransformOption = _cameraManager.GetCurrentAppliedDefaultTransformOption();
            if (defaultCameraTransformOption != 0)
            {
                commands.Add(new CameraSetDefaultTransformCommand(defaultCameraTransformOption));   
            }

            // Good to have
            commands.Add(new CameraFadeInCommand());
            return commands;
        }
    }
}