// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Combat
{
    using Audio;
    using Command;
    using Command.SceCommands;
    using Core.DataReader.Ini;
    using Core.GameBox;
    using Core.Utils;
    using Data;
    using MetaData;
    using Scene;
    using State;
    using UnityEngine;

    public sealed class CombatManager
    {
        private const string COMBAT_CONFIG_FILE_NAME = "combat.ini";
        private const string COMBAT_CAMERA_CONFIG_FILE_NAME = "cbCam.ini";

        private const float COMBAT_CAMERA_DEFAULT_FOV = 39f;

        private readonly GameResourceProvider _resourceProvider;
        private readonly Camera _mainCamera;
        private readonly SceneManager _sceneManager;
        private readonly GameStateManager _gameStateManager;

        private readonly CombatConfigFile _combatConfigFile;
        private readonly CombatCameraConfigFile _combatCameraConfigFile;

        public CombatManager(GameResourceProvider resourceProvider,
            Camera mainCamera,
            SceneManager sceneManager,
            GameStateManager gameStateManager)
        {
            _resourceProvider = Requires.IsNotNull(resourceProvider, nameof(resourceProvider));
            _mainCamera = Requires.IsNotNull(mainCamera, nameof(mainCamera));
            _sceneManager = Requires.IsNotNull(sceneManager, nameof(sceneManager));
            _gameStateManager = Requires.IsNotNull(gameStateManager, nameof(gameStateManager));

            _combatConfigFile = _resourceProvider.GetGameResourceFile<CombatConfigFile>(
                FileConstants.DataScriptFolderVirtualPath + COMBAT_CONFIG_FILE_NAME);
            _combatCameraConfigFile = _resourceProvider.GetGameResourceFile<CombatCameraConfigFile>(
                FileConstants.DataScriptFolderVirtualPath + COMBAT_CAMERA_CONFIG_FILE_NAME);
        }

        public void EnterCombat(CombatContext combatContext)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(new CameraFollowPlayerCommand(0));

            _gameStateManager.TryGoToState(GameState.Combat);

            _sceneManager.LoadCombatScene(combatContext.CombatSceneName);

            if (!string.IsNullOrEmpty(combatContext.CombatMusicName))
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new PlayMusicCommand(combatContext.CombatMusicName, -1));
            }

            SetCameraPosition(_combatCameraConfigFile.DefaultCamConfigs[0]);
        }

        private void SetCameraPosition(CombatCameraConfig config)
        {
            _mainCamera.transform.position = GameBoxInterpreter.ToUnityPosition(
                new Vector3(config.GameBoxPositionX, config.GameBoxPositionY, config.GameBoxPositionZ));

            // _mainCamera.transform.rotation = GameBoxInterpreter.ToUnityRotation(
            //     config.Pitch, config.Yaw, config.Roll);

            _mainCamera.transform.LookAt(Vector3.zero);
            _mainCamera.fieldOfView = COMBAT_CAMERA_DEFAULT_FOV;
        }

        public void Update(float deltaTime)
        {

        }
    }
}