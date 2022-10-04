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
    using Camera;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataReader.Scn;
    using Effect;
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
        private readonly BigMapManager _bigMapManager;
        private readonly ScriptManager _scriptManager;
        private readonly FavorManager _favorManager;
        private readonly CameraManager _cameraManager;
        private readonly PostProcessManager _postProcessManager;

        private const string SAVE_FILE_NAME = "save.txt";
        
        public SaveManager(SceneManager sceneManager,
            PlayerManager playerManager,
            TeamManager teamManager,
            BigMapManager bigMapManager,
            ScriptManager scriptManager,
            FavorManager favorManager,
            CameraManager cameraManager,
            PostProcessManager postProcessManager)
        {
            _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
            _playerManager = playerManager ?? throw new ArgumentNullException(nameof(playerManager));
            _teamManager = teamManager ?? throw new ArgumentNullException(nameof(teamManager));
            _bigMapManager = bigMapManager != null ? bigMapManager : throw new ArgumentNullException(nameof(bigMapManager));
            _scriptManager = scriptManager ?? throw new ArgumentNullException(nameof(scriptManager));
            _favorManager = favorManager ?? throw new ArgumentNullException(nameof(favorManager));
            _cameraManager = cameraManager != null ? cameraManager : throw new ArgumentNullException(nameof(cameraManager));
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
                new ResetGameStateCommand(),
            };
            
            commands.AddRange(varsToSave.Select(var =>
                new ScriptVarSetValueCommand(var.Key, var.Value)));
            
            commands.AddRange(new List<ICommand>()
            {
                new SceneLoadCommand(currentSceneInfo.CityName, currentSceneInfo.Name),
                new ActorActivateCommand(ActorConstants.PlayerActorVirtualID, 1),
                new ActorEnablePlayerControlCommand(ActorConstants.PlayerActorVirtualID),
                new PlayerEnableInputCommand(1),
                new ActorSetNavLayerCommand(ActorConstants.PlayerActorVirtualID,
                    playerActorMovementController.GetCurrentLayerIndex()),
                new ActorSetTilePositionCommand(ActorConstants.PlayerActorVirtualID,
                    playerActorTilePosition.x, playerActorTilePosition.y)
            });

            commands.AddRange(_teamManager.GetActorsInTeam()
                .Select(actorId => new TeamAddOrRemoveActorCommand((int) actorId, 1)));

            if (saveLevel == SaveLevel.Full)
            {
                commands.AddRange(_favorManager.GetAllActorFavorInfo()
                    .Select(favorInfo => new FavorAddCommand(favorInfo.Key, favorInfo.Value)));

                var allSceneObjectIds = currentScene.GetAllSceneObjects().Keys.ToArray();
                var allActivatedSceneObjectIds = currentScene.GetAllActivatedSceneObjects().Keys.ToArray();

                commands.AddRange(allActivatedSceneObjectIds
                    .Select(activeSceneObjectId => new SceneActivateObjectCommand(activeSceneObjectId, 1)));

                commands.AddRange(allSceneObjectIds
                    .Where(_ => !allActivatedSceneObjectIds.Contains(_))
                    .Select(inactiveSceneObjectId => new SceneActivateObjectCommand(inactiveSceneObjectId, 0)));

                commands.AddRange(currentScene.GetAllActors()
                    .Select(actorInfo => actorInfo.Value.GetComponent<ActorController>().IsActive
                        ? new ActorActivateCommand(actorInfo.Key, 1)
                        : new ActorActivateCommand(actorInfo.Key, 0)));
            }

            #if PAL3
            var longKuiCurrentMode = currentScene.GetActorGameObject((byte) PlayerActorId.LongKui)
                .GetComponent<LongKuiController>()
                .GetCurrentMode();
            if (longKuiCurrentMode != 0)
            {
                commands.Add(new LongKuiSwitchModeCommand(longKuiCurrentMode));
            }
            #endif
            
            commands.AddRange(_bigMapManager.GetRegionEnablementInfo()
                .Select(regionEnablement => new BigMapEnableRegionCommand(regionEnablement.Key, regionEnablement.Value)));

            var currentEffectMode = _postProcessManager.GetCurrentAppliedEffectMode();
            if (currentEffectMode != -1)
            {
                commands.Add(new EffectSetScreenEffectCommand(currentEffectMode));   
            }

            var defaultCameraTransformOption = _cameraManager.GetCurrentAppliedDefaultTransformOption();
            if (defaultCameraTransformOption != 0)
            {
                commands.Add(new CameraSetDefaultTransformCommand(defaultCameraTransformOption));   
            }

            commands.Add(new CameraFadeInCommand());

            return commands;
        }
    }
}