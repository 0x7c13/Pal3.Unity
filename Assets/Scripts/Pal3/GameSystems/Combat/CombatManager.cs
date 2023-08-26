// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.GameSystems.Combat
{
    using System;
    using System.Collections.Generic;
    using Command;
    using Command.SceCommands;
    using Core.Contracts;
    using Core.DataReader.Gdb;
    using Core.DataReader.Ini;
    using Core.GameBox;
    using Core.Utils;
    using Data;
    using MetaData;
    using Scene;
    using State;
    using Team;
    using UnityEngine;
    using UnityEngine.InputSystem;

    public sealed class CombatManager
    {
        public event EventHandler<bool> OnCombatFinished;

        private const string COMBAT_CAMERA_CONFIG_FILE_NAME = "cbCam.ini";
        private const float COMBAT_CAMERA_DEFAULT_FOV = 39f;

        private readonly TeamManager _teamManager;
        private readonly Camera _mainCamera;
        private readonly SceneManager _sceneManager;
        private readonly GameStateManager _gameStateManager;

        private readonly IDictionary<int, CombatActorInfo> _combatActorInfos;
        private readonly CombatCameraConfigFile _combatCameraConfigFile;

        private Vector3 _cameraPositionBeforeCombat;
        private Quaternion _cameraRotationBeforeCombat;
        private float _cameraFovBeforeCombat;

        public CombatManager(GameResourceProvider resourceProvider,
            TeamManager teamManager,
            Camera mainCamera,
            SceneManager sceneManager)
        {
            Requires.IsNotNull(resourceProvider, nameof(resourceProvider));
            _teamManager = Requires.IsNotNull(teamManager, nameof(teamManager));
            _mainCamera = Requires.IsNotNull(mainCamera, nameof(mainCamera));
            _sceneManager = Requires.IsNotNull(sceneManager, nameof(sceneManager));

            _combatActorInfos = resourceProvider.GetCombatActorInfos();
            _combatCameraConfigFile = resourceProvider.GetGameResourceFile<CombatCameraConfigFile>(
                FileConstants.DataScriptFolderVirtualPath + COMBAT_CAMERA_CONFIG_FILE_NAME);
        }

        public void EnterCombat(CombatContext combatContext)
        {
            _mainCamera.transform.GetPositionAndRotation(out _cameraPositionBeforeCombat,
                out _cameraRotationBeforeCombat);
            _cameraFovBeforeCombat = _mainCamera.fieldOfView;

            if (!string.IsNullOrEmpty(combatContext.CombatMusicName))
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new PlayScriptMusicCommand(combatContext.CombatMusicName, -1));
            }

            CombatScene scene = _sceneManager.LoadCombatScene(combatContext.CombatSceneName);

            Dictionary<int, CombatActorInfo> monsterActors = new ();

            for (int i = 0; i < combatContext.MonsterIds.Length; i++)
            {
                var monsterActorId = combatContext.MonsterIds[i];
                if (monsterActorId == 0) continue;
                monsterActors[i] = _combatActorInfos[(int)monsterActorId];
            }

            Dictionary<int, CombatActorInfo> playerActors = new ();
            int positionIndex = 0;
            foreach (PlayerActorId playerActorId in _teamManager.GetActorsInTeam())
            {
                var combatActorId = ActorConstants.MainActorCombatActorIdMap[playerActorId];
                playerActors[positionIndex++] = _combatActorInfos[combatActorId];
            }

            scene.LoadActors(monsterActors, playerActors);

            SetCameraPosition(_combatCameraConfigFile.DefaultCamConfigs[0]);

            CommandDispatcher<ICommand>.Instance.Dispatch(new CameraFadeInCommand());
        }

        public void ExitCombat()
        {
            // Stop combat music
            CommandDispatcher<ICommand>.Instance.Dispatch(new StopScriptMusicCommand());
            _sceneManager.UnloadCombatScene();
            ResetCameraPosition();
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
                OnCombatFinished?.Invoke(this, true);
            }
            else if (Keyboard.current.f2Key.wasPressedThisFrame)
            {
                OnCombatFinished?.Invoke(this, false);
            }
        }
    }
}