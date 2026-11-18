// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.GameSystems.Battle
{
    using System;
    using System.Collections.Generic;
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
    using Engine.Services;
    using Game.Scene;
    using Scene;
    using Team;
    using UnityEngine.InputSystem;

    using Quaternion = UnityEngine.Quaternion;
    using Vector3 = UnityEngine.Vector3;

    public sealed class BattleResult
    {
        public bool IsPlayerWin
        {
            get; set;
        }

        public BattleContext BattleContext
        {
            get; set;
        }
    }

    public sealed class BattleManager
    {
        public event EventHandler<BattleResult> OnBattleFinished;

        private const string COMBAT_CAMERA_CONFIG_FILE_NAME = "cbCam.ini";
        private const float COMBAT_CAMERA_DEFAULT_FOV = 39f;

        private readonly TeamManager _teamManager;
        private readonly SceneManager _sceneManager;
        private readonly CameraManager _cameraManager;

        private readonly IDictionary<int, CombatActorInfo> _battleActorInfos;
        private readonly CombatCameraConfigFile _battleCameraConfigFile;

        private Vector3 _cameraPositionBeforeBattle;
        private Quaternion _cameraRotationBeforeBattle;
        private float _cameraFovBeforeBattle;

        private BattleScene _battleScene;
        private BattleContext _currentBattleContext;

        public BattleManager(GameResourceProvider resourceProvider,
            TeamManager teamManager,
            CameraManager cameraManager,
            SceneManager sceneManager)
        {
            Requires.IsNotNull(resourceProvider, nameof(resourceProvider));
            _teamManager = Requires.IsNotNull(teamManager, nameof(teamManager));
            _cameraManager = Requires.IsNotNull(cameraManager, nameof(cameraManager));
            _sceneManager = Requires.IsNotNull(sceneManager, nameof(sceneManager));

            _battleActorInfos = resourceProvider.GetCombatActorInfos();
            _battleCameraConfigFile = resourceProvider.GetGameResourceFile<CombatCameraConfigFile>(
                FileConstants.DataScriptFolderVirtualPath + COMBAT_CAMERA_CONFIG_FILE_NAME);
        }

        public void EnterBattle(BattleContext combatContext)
        {
            _currentBattleContext = combatContext;

            _cameraManager.GetCameraTransform().GetPositionAndRotation(out _cameraPositionBeforeBattle,
                out _cameraRotationBeforeBattle);

            _cameraFovBeforeBattle = _cameraManager.GetFieldOfView();

            string previousMusic = Engine.Services.ServiceLocator.Instance.Get<Audio.AudioManager>().GetCurrentScriptMusic();
            try
            {
                if (!string.IsNullOrEmpty(combatContext.BattleMusicName))
                {
                    Pal3.Instance.Execute(new PlayScriptMusicCommand(combatContext.BattleMusicName, -1));
                }

                _battleScene = _sceneManager.LoadBattleScene(combatContext.BattleSceneName);

                Dictionary<ElementPosition, CombatActorInfo> combatActors = new();

                int positionIndex = 0;
                foreach (PlayerActorId playerActorId in _teamManager.GetActorsInTeam())
                {
                    int combatActorId = ActorConstants.MainActorCombatActorIdMap[playerActorId];
                    CombatActorInfo info = CopyActorInfo(_battleActorInfos[combatActorId]);
                    info.Level = ServiceLocator.Instance.Get<PlayerProgressManager>().GetLevel((int)playerActorId);
#if PAL3A
                    if (playerActorId == PlayerActorId.NanGongHuang && combatContext.NanGongHuangStartsTransformed)
                        info.ModelId = ActorConstants.NanGongHuangWolfModeActorName;
                    if (playerActorId == PlayerActorId.WangPengXu && combatContext.WangPengXuStartsTransformed)
                    {
                        info.ModelId = "106";
                        info.AttributeValues[ActorAttributeType.Hp] = 400;
                        info.AttributeValues[ActorAttributeType.Defense] *= 2;
                    }
#endif
                    combatActors[(ElementPosition)positionIndex++] = info;
                }

                for (int i = 0; i < combatContext.EnemyIds.Length; i++)
                {
                    uint enemyActorId = combatContext.EnemyIds[i];
                    if (enemyActorId == 0)
                        continue;
                    CombatActorInfo info = CopyActorInfo(_battleActorInfos[(int)enemyActorId]);
#if PAL3A
                    if (enemyActorId == 722)
                    {
                        info.AttributeValues[ActorAttributeType.Hp] = AddAttributeBonus(
                            info.AttributeValues[ActorAttributeType.Hp], combatContext.LiaoRiHpBonus);
                        info.AttributeValues[ActorAttributeType.Mp] = AddAttributeBonus(
                            info.AttributeValues[ActorAttributeType.Mp], combatContext.LiaoRiMpBonus);
                    }
#endif
                    combatActors[(ElementPosition)((int)ElementPosition.EnemyWater + i)] = info;
                }

                _battleScene.LoadActors(combatActors, combatContext.MeetType);

                SetCameraPosition(_battleCameraConfigFile.DefaultCamConfigs[0]);

                Pal3.Instance.Execute(new CameraFadeInCommand());

                if (combatContext.MeetType != MeetType.RunningIntoEachOther)
                {
                    Pal3.Instance.Execute(new UIDisplayNoteCommand(
                        combatContext.MeetType == MeetType.PlayerChasingEnemy
                            ? "偷袭敌方成功！"
                            : "被敌人偷袭！"));
                }
            }
            catch
            {
                _sceneManager.UnloadBattleScene();
                ResetCameraPosition();
                if (!string.IsNullOrEmpty(combatContext.BattleMusicName))
                {
                    if (string.IsNullOrEmpty(previousMusic))
                        Pal3.Instance.Execute(new StopScriptMusicCommand());
                    else
                        Pal3.Instance.Execute(new PlayScriptMusicCommand(previousMusic, -1));
                }
                _battleScene = null;
                _currentBattleContext = null;
                throw;
            }
        }

        private static CombatActorInfo CopyActorInfo(CombatActorInfo info)
        {
            info.AttributeValues = new Dictionary<ActorAttributeType, int>(info.AttributeValues);
            if (info.Type == CombatActorType.MainActor)
                info.ModelId = info.Id.ToString();
            return info;
        }

#if PAL3A
        private static int AddAttributeBonus(int value, int bonus)
        {
            return (int)Math.Clamp((long)value + bonus, 0, int.MaxValue);
        }
#endif

        public void ExitBattle()
        {
            // Stop combat music
            Pal3.Instance.Execute(new StopScriptMusicCommand());
            Pal3.Instance.Execute(new CameraFadeInCommand());
            _sceneManager.UnloadBattleScene();
            ResetCameraPosition();
            _battleScene = null;
            _currentBattleContext = null;
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
            _cameraManager.GetCameraTransform().SetPositionAndRotation(_cameraPositionBeforeBattle,
                _cameraRotationBeforeBattle);

            Pal3.Instance.Execute(new CameraSetFieldOfViewCommand(_cameraFovBeforeBattle));
        }

        public void Update(float deltaTime)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _battleScene == null)
                return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                OnBattleFinished?.Invoke(this, new BattleResult
                {
                    IsPlayerWin = true,
                    BattleContext = _currentBattleContext,
                });
                return;
            }

            ElementPosition first = keyboard.ctrlKey.isPressed ? ElementPosition.EnemyWater : ElementPosition.AllyWater;
            for (var key = Key.F1; key <= Key.F6; key++)
            {
                if (!keyboard[key].wasPressedThisFrame)
                    continue;

                var slot = (ElementPosition)((int)first + (int)key - (int)Key.F1);
                var attacker = _battleScene.GetBattleActorController(slot);
                if (attacker != null && _battleScene.GetRandomOpponent(slot) is { } target)
                    attacker.StartCoroutine(attacker.StartNormalAttackAsync(target, _battleScene));
                return;
            }
        }
    }
}
