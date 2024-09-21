// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.GameSystems.Combat
{
    using System;
    using System.Collections.Generic;
    using Actor.Controllers;
    using Camera;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Gdb;
    using Core.DataReader.Ini;
    using Core.Primitives;
    using Core.Utilities;
    using Data;
    using Engine.Core.Abstraction;
    using Engine.Extensions;
    using Game.Scene;
    using Scene;
    using State;
    using Team;
    using UnityEngine.InputSystem;

    using Quaternion = UnityEngine.Quaternion;
    using Vector3 = UnityEngine.Vector3;

    public sealed class CombatResult
    {
        public bool IsPlayerWin { get; set; }
        public CombatContext CombatContext { get; set; }
    }

    public sealed class CombatManager
    {
        public event EventHandler<CombatResult> OnCombatFinished;

        private const string COMBAT_CAMERA_CONFIG_FILE_NAME = "cbCam.ini";
        private const float COMBAT_CAMERA_DEFAULT_FOV = 39f;

        private readonly TeamManager _teamManager;
        private readonly SceneManager _sceneManager;
        private readonly CameraManager _cameraManager;
        private readonly GameStateManager _gameStateManager;

        private readonly IDictionary<int, CombatActorInfo> _combatActorInfos;
        private readonly CombatCameraConfigFile _combatCameraConfigFile;

        private Vector3 _cameraPositionBeforeCombat;
        private Quaternion _cameraRotationBeforeCombat;
        private float _cameraFovBeforeCombat;

        private CombatScene _combatScene;
        private CombatContext _currentCombatContext;

        public CombatManager(GameResourceProvider resourceProvider,
            TeamManager teamManager,
            CameraManager cameraManager,
            SceneManager sceneManager)
        {
            Requires.IsNotNull(resourceProvider, nameof(resourceProvider));
            _teamManager = Requires.IsNotNull(teamManager, nameof(teamManager));
            _cameraManager = Requires.IsNotNull(cameraManager, nameof(cameraManager));
            _sceneManager = Requires.IsNotNull(sceneManager, nameof(sceneManager));

            _combatActorInfos = resourceProvider.GetCombatActorInfos();
            _combatCameraConfigFile = resourceProvider.GetGameResourceFile<CombatCameraConfigFile>(
                FileConstants.DataScriptFolderVirtualPath + COMBAT_CAMERA_CONFIG_FILE_NAME);
        }

        public void EnterCombat(CombatContext combatContext)
        {
            _currentCombatContext = combatContext;

            _cameraManager.GetCameraTransform().GetPositionAndRotation(out _cameraPositionBeforeCombat,
                out _cameraRotationBeforeCombat);

            _cameraFovBeforeCombat = _cameraManager.GetFieldOfView();

            if (!string.IsNullOrEmpty(combatContext.CombatMusicName))
            {
                Pal3.Instance.Execute(new PlayScriptMusicCommand(combatContext.CombatMusicName, -1));
            }

            _combatScene = _sceneManager.LoadCombatScene(combatContext.CombatSceneName);

            Dictionary<ElementPosition, CombatActorInfo> combatActors = new ();

            int positionIndex = 0;
            foreach (PlayerActorId playerActorId in _teamManager.GetActorsInTeam())
            {
                int combatActorId = ActorConstants.MainActorCombatActorIdMap[playerActorId];
                combatActors[(ElementPosition)positionIndex++] = _combatActorInfos[combatActorId];
            }

            for (int i = 0; i < combatContext.EnemyIds.Length; i++)
            {
                uint enemyActorId = combatContext.EnemyIds[i];
                if (enemyActorId == 0) continue;
                combatActors[(ElementPosition)((int)ElementPosition.EnemyWater + i)] =
                    _combatActorInfos[(int)enemyActorId];
            }

            _combatScene.LoadActors(combatActors, combatContext.MeetType);

            SetCameraPosition(_combatCameraConfigFile.DefaultCamConfigs[0]);

            Pal3.Instance.Execute(new CameraFadeInCommand());

            if (combatContext.MeetType != MeetType.RunningIntoEachOther)
            {
                Pal3.Instance.Execute(new UIDisplayNoteCommand(
                    combatContext.MeetType == MeetType.PlayerChasingEnemy
                        ? "偷袭敌方成功！"
                        : "被敌人偷袭！"));
            }
        }

        public void ExitCombat()
        {
            // Stop combat music
            Pal3.Instance.Execute(new StopScriptMusicCommand());
            Pal3.Instance.Execute(new CameraFadeInCommand());
            _sceneManager.UnloadCombatScene();
            ResetCameraPosition();
        }

        private void SetCameraPosition(CombatCameraConfig config)
        {
            Vector3 cameraPosition = new GameBoxVector3(
                    config.GameBoxPositionX,
                    config.GameBoxPositionY,
                    config.GameBoxPositionZ).ToUnityPosition();

            ITransform cameraTransform = _cameraManager.GetCameraTransform();
            cameraTransform.Position = cameraPosition;
            // cameraTransform.Rotation = UnityPrimitivesConvertor.ToUnityQuaternion(
            //     config.Pitch, config.Yaw, config.Roll);
            cameraTransform.LookAt(Vector3.zero);

            Pal3.Instance.Execute(new CameraSetFieldOfViewCommand(COMBAT_CAMERA_DEFAULT_FOV));
        }

        private void ResetCameraPosition()
        {
            _cameraManager.GetCameraTransform().SetPositionAndRotation(_cameraPositionBeforeCombat,
                _cameraRotationBeforeCombat);

            Pal3.Instance.Execute(new CameraSetFieldOfViewCommand(_cameraFovBeforeCombat));
        }

        public void Update(float deltaTime)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                OnCombatFinished?.Invoke(this, new CombatResult
                {
                    IsPlayerWin = true,
                    CombatContext = _currentCombatContext,
                });
            }

            CombatActorController fromController = null;
            int fromPosition = (int)ElementPosition.AllyWater;

            if (Keyboard.current.ctrlKey.isPressed)
            {
                fromPosition = (int)ElementPosition.EnemyWater;
            }

            if (Keyboard.current.f1Key.wasPressedThisFrame)
            {
                fromController = _combatScene.GetCombatActorController((ElementPosition)fromPosition);
            }
            else if (Keyboard.current.f2Key.wasPressedThisFrame)
            {
                fromController = _combatScene.GetCombatActorController((ElementPosition)(fromPosition + 1));
            }
            else if (Keyboard.current.f3Key.wasPressedThisFrame)
            {
                fromController = _combatScene.GetCombatActorController((ElementPosition)(fromPosition + 2));
            }
            else if (Keyboard.current.f4Key.wasPressedThisFrame)
            {
                fromController = _combatScene.GetCombatActorController((ElementPosition)(fromPosition + 3));
            }
            else if (Keyboard.current.f5Key.wasPressedThisFrame)
            {
                fromController = _combatScene.GetCombatActorController((ElementPosition)(fromPosition + 4));
            }
            else if (Keyboard.current.f6Key.wasPressedThisFrame)
            {
                fromController = _combatScene.GetCombatActorController((ElementPosition)(fromPosition + 5));
            }

            if (fromController != null)
            {
                CombatActorController toController = null;

                while (true)
                {
                    int min = (int) (fromPosition == 0 ? ElementPosition.EnemyWater : ElementPosition.AllyWater);
                    int max = (int) (fromPosition == 0 ? ElementPosition.EnemyCenter : ElementPosition.AllyCenter);

                    ElementPosition toPosition = (ElementPosition)RandomGenerator.Range(min, max + 1);

                    if (_combatScene.GetCombatActorController(toPosition) is {} controller)
                    {
                        toController = controller;
                        break;
                    }
                }

                Pal3.Instance.StartCoroutine(fromController.StartNormalAttackAsync(toController, _combatScene));
            }
        }
    }
}