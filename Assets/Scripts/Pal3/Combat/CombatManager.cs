// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Combat
{
    using System;
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
    using UnityEngine.InputSystem;

    public sealed class CombatManager
    {
        public event EventHandler<bool> OnCombatFinished;

        private const string COMBAT_CONFIG_FILE_NAME = "combat.ini";
        private const string COMBAT_CAMERA_CONFIG_FILE_NAME = "cbCam.ini";

        private const float COMBAT_CAMERA_DEFAULT_FOV = 39f;

        private readonly GameResourceProvider _resourceProvider;
        private readonly Camera _mainCamera;
        private readonly SceneManager _sceneManager;
        private readonly GameStateManager _gameStateManager;

        private readonly CombatConfigFile _combatConfigFile;
        private readonly CombatCameraConfigFile _combatCameraConfigFile;

        private Vector3 _cameraPositionBeforeCombat;
        private Quaternion _cameraRotationBeforeCombat;
        private float _cameraFovBeforeCombat;

        public CombatManager(GameResourceProvider resourceProvider,
            Camera mainCamera,
            SceneManager sceneManager)
        {
            _resourceProvider = Requires.IsNotNull(resourceProvider, nameof(resourceProvider));
            _mainCamera = Requires.IsNotNull(mainCamera, nameof(mainCamera));
            _sceneManager = Requires.IsNotNull(sceneManager, nameof(sceneManager));

            _combatConfigFile = _resourceProvider.GetGameResourceFile<CombatConfigFile>(
                FileConstants.DataScriptFolderVirtualPath + COMBAT_CONFIG_FILE_NAME);
            _combatCameraConfigFile = _resourceProvider.GetGameResourceFile<CombatCameraConfigFile>(
                FileConstants.DataScriptFolderVirtualPath + COMBAT_CAMERA_CONFIG_FILE_NAME);
        }

        public void EnterCombat(CombatContext combatContext)
        {
            _mainCamera.transform.GetPositionAndRotation(out _cameraPositionBeforeCombat,
                out _cameraRotationBeforeCombat);
            _cameraFovBeforeCombat = _mainCamera.fieldOfView;

            CommandDispatcher<ICommand>.Instance.Dispatch(new CameraFollowPlayerCommand(0));
            CommandDispatcher<ICommand>.Instance.Dispatch(new CameraFadeInCommand());

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
            var cameraPosition = GameBoxInterpreter.ToUnityPosition(
                new Vector3(config.GameBoxPositionX, config.GameBoxPositionY, config.GameBoxPositionZ));

            _mainCamera.transform.position = cameraPosition;

            // _mainCamera.transform.rotation = GameBoxInterpreter.ToUnityRotation(
            //     config.Pitch, config.Yaw, config.Roll);

            _mainCamera.transform.LookAt(Vector3.zero);
            _mainCamera.fieldOfView = COMBAT_CAMERA_DEFAULT_FOV;
        }

        private void ResetCameraPosition()
        {
            _mainCamera.transform.SetPositionAndRotation(_cameraPositionBeforeCombat,
                _cameraRotationBeforeCombat);
            _mainCamera.fieldOfView = _cameraFovBeforeCombat;
        }

        public void Update(float deltaTime)
        {
            if (Keyboard.current.f1Key.wasPressedThisFrame)
            {
                ResetCameraPosition();
                OnCombatFinished?.Invoke(this, true);
            }
            else if (Keyboard.current.f2Key.wasPressedThisFrame)
            {
                ResetCameraPosition();
                OnCombatFinished?.Invoke(this, false);
            }
        }
    }
}