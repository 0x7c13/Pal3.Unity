// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.GamePlay
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Actor;
    using Actor.Controllers;
    using Camera;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Core.Utilities;
    using Data;
    using Engine.Core.Abstraction;
    using Engine.Logging;
    using Engine.Services;
    using GameSystems.Team;
    using Input;
    using Scene;
    using Scene.SceneObjects;
    using State;
    using UnityEngine.InputSystem;

    using Quaternion = UnityEngine.Quaternion;
    using Vector2 = UnityEngine.Vector2;
    using Vector2Int = UnityEngine.Vector2Int;
    using Vector3 = UnityEngine.Vector3;

    public partial class PlayerGamePlayManager : IDisposable,
        ICommandExecutor<ActorEnablePlayerControlCommand>,
        ICommandExecutor<ActorSetTilePositionCommand>,
        ICommandExecutor<PlayerEnableInputCommand>,
        #if PAL3
        ICommandExecutor<LongKuiSwitchModeCommand>,
        #endif
        ICommandExecutor<SceneLeavingCurrentSceneNotification>,
        ICommandExecutor<ScenePostLoadingNotification>,
        ICommandExecutor<PlayerActorLookAtSceneObjectCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private readonly GameResourceProvider _resourceProvider;
        private readonly GameStateManager _gameStateManager;
        private readonly PlayerActorManager _playerActorManager;
        private readonly TeamManager _teamManager;
        private readonly PlayerInputActions _inputActions;
        private readonly SceneManager _sceneManager;
        private readonly CameraManager _cameraManager;
        private readonly IPhysicsManager _physicsManager;

        private string _currentMovementSfxAudioName = string.Empty;

        private Vector2? _lastInputTapPosition;
        private bool _isTilePositionPendingNotify;

        private string _lastKnownPlayerActorAction = string.Empty;

        #if PAL3
        private int _longKuiLastKnownMode = 0;
        #endif

        private GameActor _playerActor;
        private IGameEntity _playerActorGameEntity;
        private ActorController _playerActorController;
        private ActorActionController _playerActorActionController;
        private ActorMovementController _playerActorMovementController;

        private const int LAST_KNOWN_SCENE_STATE_LIST_MAX_LENGTH = 2;
        private readonly List<(ScnSceneInfo sceneInfo,
            int actorNavIndex,
            Vector2Int actorTilePosition,
            Vector3 actorFacing)> _playerActorLastKnownSceneState = new ();

        public PlayerGamePlayManager(GameResourceProvider resourceProvider,
            GameStateManager gameStateManager,
            PlayerActorManager playerActorManager,
            TeamManager teamManager,
            PlayerInputActions inputActions,
            SceneManager sceneManager,
            CameraManager cameraManager,
            IPhysicsManager physicsManager)
        {
            _resourceProvider = Requires.IsNotNull(resourceProvider, nameof(resourceProvider));
            _gameStateManager = Requires.IsNotNull(gameStateManager, nameof(gameStateManager));
            _playerActorManager = Requires.IsNotNull(playerActorManager, nameof(playerActorManager));
            _teamManager = Requires.IsNotNull(teamManager, nameof(teamManager));
            _inputActions = Requires.IsNotNull(inputActions, nameof(inputActions));
            _sceneManager = Requires.IsNotNull(sceneManager, nameof(sceneManager));
            _cameraManager = Requires.IsNotNull(cameraManager, nameof(cameraManager));
            _physicsManager = Requires.IsNotNull(physicsManager, nameof(physicsManager));

            _inputActions.Gameplay.OnTap.performed += OnTapPerformed;
            _inputActions.Gameplay.OnMove.performed += OnMovePerformed;
            _inputActions.Gameplay.OnDoubleTap.performed += OnDoubleTapPerformed;
            _inputActions.Gameplay.PortalTo.performed += PortalToPerformed;
            _inputActions.Gameplay.Interact.performed += InteractionPerformed;
            _inputActions.Gameplay.Movement.canceled += MovementCanceled;
            _inputActions.Gameplay.SwitchToPreviousPlayerActor.performed += SwitchToPreviousPlayerActorPerformed;
            _inputActions.Gameplay.SwitchToNextPlayerActor.performed += SwitchToNextPlayerActorPerformed;

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            _inputActions.Gameplay.OnTap.performed -= OnTapPerformed;
            _inputActions.Gameplay.OnMove.performed -= OnMovePerformed;
            _inputActions.Gameplay.OnDoubleTap.performed -= OnDoubleTapPerformed;
            _inputActions.Gameplay.PortalTo.performed -= PortalToPerformed;
            _inputActions.Gameplay.Interact.performed -= InteractionPerformed;
            _inputActions.Gameplay.Movement.canceled -= MovementCanceled;
            _inputActions.Gameplay.SwitchToPreviousPlayerActor.performed -= SwitchToPreviousPlayerActorPerformed;
            _inputActions.Gameplay.SwitchToNextPlayerActor.performed -= SwitchToNextPlayerActorPerformed;
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Update(float deltaTime)
        {
            if (_playerActorGameEntity == null) return;

            bool isPlayerInControl = false;

            if (_gameStateManager.GetCurrentState() == GameState.Gameplay &&
                _playerActorManager.IsPlayerInputEnabled() &&
                _playerActorManager.IsPlayerActorControlEnabled())
            {
                isPlayerInControl = true;
                ReadInputAndMovePlayerIfNeeded(deltaTime);
            }

            bool shouldUpdatePlayerActorMovementSfx = false;

            Vector3 position = _playerActorGameEntity.Transform.Position;
            int layerIndex = _playerActorMovementController.GetCurrentLayerIndex();

            if (!(position == _playerActorManager.LastKnownPosition &&
                  layerIndex == _playerActorManager.LastKnownLayerIndex))
            {
                _playerActorManager.LastKnownPosition = position;
                Vector2Int tilePosition = _playerActorMovementController.GetTilePosition();
                if (!(tilePosition == _playerActorManager.LastKnownTilePosition &&
                      layerIndex == _playerActorManager.LastKnownLayerIndex))
                {
                    shouldUpdatePlayerActorMovementSfx = true;
                    _playerActorManager.LastKnownTilePosition = tilePosition;
                    PlayerActorTilePositionChanged(layerIndex, tilePosition, !isPlayerInControl);
                }
                else if (_isTilePositionPendingNotify)
                {
                    PlayerActorTilePositionChanged(layerIndex, tilePosition, !isPlayerInControl);
                    _isTilePositionPendingNotify = false;
                }
            }

            _playerActorManager.LastKnownLayerIndex = layerIndex;

            string currentAction = _playerActorActionController.GetCurrentAction();
            if (currentAction != _lastKnownPlayerActorAction)
            {
                _lastKnownPlayerActorAction = currentAction;
                shouldUpdatePlayerActorMovementSfx = true;
            }

            if (shouldUpdatePlayerActorMovementSfx)
            {
                UpdatePlayerActorMovementSfx(_lastKnownPlayerActorAction);
            }
        }

        private void PlayerActorTilePositionChanged(int layerIndex, Vector2Int tilePosition, bool movedByScript)
        {
            Pal3.Instance.Execute(new PlayerActorTilePositionUpdatedNotification(
                    tilePosition.x,
                    tilePosition.y,
                    layerIndex,
                    movedByScript));
        }

        private void MovementCanceled(InputAction.CallbackContext _)
        {
            _playerActorActionController.PerformAction(ActorActionType.Stand);
        }

        private void OnTapPerformed(InputAction.CallbackContext _)
        {
            if (!_playerActorManager.IsPlayerActorControlEnabled() ||
                !_playerActorManager.IsPlayerInputEnabled())
            {
                return;
            }

            MoveToTapPosition(false);
        }

        private void OnDoubleTapPerformed(InputAction.CallbackContext _)
        {
            if (!_playerActorManager.IsPlayerActorControlEnabled() ||
                !_playerActorManager.IsPlayerInputEnabled())
            {
                return;
            }

            MoveToTapPosition(true);
        }

        private void PortalToPerformed(InputAction.CallbackContext _)
        {
            if (!_playerActorManager.IsPlayerActorControlEnabled() ||
                !_playerActorManager.IsPlayerInputEnabled())
            {
                return;
            }

            if (Keyboard.current.leftCtrlKey.isPressed)
            {
                PortalToTapPosition();
            }
            else
            {
                if (IsPlayerActorInsideJumpableArea())
                {
                    JumpToTapPosition();
                }
            }
        }

        private void OnMovePerformed(InputAction.CallbackContext ctx)
        {
            if (!_playerActorManager.IsPlayerActorControlEnabled() ||
                !_playerActorManager.IsPlayerInputEnabled())
            {
                return;
            }

            _lastInputTapPosition = ctx.ReadValue<Vector2>();
        }

        public void Execute(PlayerActorLookAtSceneObjectCommand command)
        {
            ITransform actorTransform = _playerActorGameEntity.Transform;
            SceneObject sceneObject = _sceneManager.GetCurrentScene().GetSceneObject(command.SceneObjectId);

            if (sceneObject?.GetGameEntity() is { } sceneObjectGameEntity)
            {
                Vector3 objectPosition = sceneObjectGameEntity.Transform.Position;

                actorTransform.LookAt(new Vector3(
                    objectPosition.x,
                    actorTransform.Position.y,
                    objectPosition.z));
            }
        }

        public void Execute(ActorEnablePlayerControlCommand command)
        {
            // Check if actor is player actor.
            if (!Enum.IsDefined(typeof(PlayerActorId), command.ActorId))
            {
                EngineLogger.LogError("Cannot enable player control for actor " +
                               $"{command.ActorId} since actor is not a player actor");
                return;
            }

            // Stop & dispose current player actor movement sfx
            if (_playerActorGameEntity != null)
            {
                _currentMovementSfxAudioName = string.Empty;
                Pal3.Instance.Execute(new StopSfxPlayingAtGameEntityRequest(_playerActorGameEntity,
                        AudioConstants.PlayerActorMovementSfxAudioSourceName,
                        disposeSource: true));
            }

            // Reset & dispose current player actor's jump indicators
            int jumpableAreaEnterCount = _jumpableAreaEnterCount;
            ResetAndDisposeJumpIndicators();

            Vector3? lastActivePlayerActorPosition = null;
            Quaternion? lastActivePlayerActorRotation = null;
            int? lastActivePlayerActorNavLayerIndex = null;

            // Deactivate current player actor
            if (_playerActorGameEntity != null &&
                _playerActor.Id != command.ActorId &&
                _playerActorController != null &&
                _playerActorController.IsActive)
            {
                lastActivePlayerActorNavLayerIndex = _playerActorMovementController.GetCurrentLayerIndex();
                _playerActorGameEntity.Transform.GetPositionAndRotation(out Vector3 currentPosition, out Quaternion currentRotation);
                lastActivePlayerActorPosition = currentPosition;
                lastActivePlayerActorRotation = currentRotation;
                Pal3.Instance.Execute(new ActorActivateCommand(_playerActor.Id, 0));
            }

            // Set target actor as player actor
            _playerActor = _sceneManager.GetCurrentScene().GetActor(command.ActorId);
            _playerActorGameEntity = _sceneManager.GetCurrentScene().GetActorGameEntity(command.ActorId);
            _playerActorController = _playerActorGameEntity.GetComponent<ActorController>();
            _playerActorActionController = _playerActorGameEntity.GetComponent<ActorActionController>();
            _playerActorMovementController = _playerActorGameEntity.GetComponent<ActorMovementController>();

            #if PAL3
            // LongKui should stay blue form when player control is enabled
            if (command.ActorId == (int)PlayerActorId.LongKui)
            {
                Pal3.Instance.Execute(new LongKuiSwitchModeCommand(0));
            }
            #endif

            // Just to make sure the new actor is activated
            if (!_playerActorController.IsActive)
            {
                Pal3.Instance.Execute(new ActorActivateCommand(_playerActor.Id, 1));

                // Inherent nav layer index
                if (lastActivePlayerActorNavLayerIndex.HasValue)
                {
                    _playerActorMovementController.SetNavLayer(lastActivePlayerActorNavLayerIndex.Value);
                }
                // Inherent position
                if (lastActivePlayerActorPosition.HasValue)
                {
                    _playerActorGameEntity.Transform.Position = lastActivePlayerActorPosition.Value;
                }
                // Inherent rotation
                if (lastActivePlayerActorRotation.HasValue)
                {
                    _playerActorGameEntity.Transform.Rotation = lastActivePlayerActorRotation.Value;
                }
            }

            // Reset states
            _playerActorManager.LastKnownPosition = null;
            _playerActorManager.LastKnownTilePosition = null;
            _playerActorManager.LastKnownLayerIndex = null;
            _lastKnownPlayerActorAction = string.Empty;

            // Inherent jumpable state
            if (jumpableAreaEnterCount > 0)
            {
                PlayerActorEnteredJumpableArea();
                // Inherent jumpable area enter count
                _jumpableAreaEnterCount = jumpableAreaEnterCount;
            }
        }

        public void Execute(PlayerEnableInputCommand command)
        {
            if (command.Enable == 0)
            {
                if (_playerActorMovementController != null)
                {
                    _playerActorMovementController.CancelMovement();
                }

                if (_playerActorActionController != null &&
                    !string.IsNullOrEmpty(_playerActorActionController.GetCurrentAction()) &&
                    !string.Equals(ActorConstants.ActionToNameMap[ActorActionType.Stand],
                        _playerActorActionController.GetCurrentAction(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    _playerActorActionController.PerformAction(ActorActionType.Stand);
                }

                _playerActorManager.LastKnownPosition = null;
                _playerActorManager.LastKnownTilePosition = null;
                _playerActorManager.LastKnownLayerIndex = null;
            }
        }

        public void Execute(ActorSetTilePositionCommand command)
        {
            if (_playerActor != null && _playerActor.Id == command.ActorId)
            {
                _isTilePositionPendingNotify = true;
            }
        }

        public void Execute(SceneLeavingCurrentSceneNotification command)
        {
            if (_sceneManager.GetCurrentScene() is not { } currentScene) return;

            // Stop current player actor movement sfx
            if (_playerActorGameEntity != null)
            {
                _currentMovementSfxAudioName = string.Empty;
                Pal3.Instance.Execute(new StopSfxPlayingAtGameEntityRequest(_playerActorGameEntity,
                        AudioConstants.PlayerActorMovementSfxAudioSourceName,
                        disposeSource: true));
            }

            // Remove game play indicators
            ResetAndDisposeJumpIndicators();

            _playerActorLastKnownSceneState.Add((
                currentScene.GetSceneInfo(),
                _playerActorMovementController.GetCurrentLayerIndex(),
                _playerActorMovementController.GetTilePosition(),
                _playerActorGameEntity != null 
                    ? _playerActorGameEntity.Transform.Forward
                    : Vector3.forward));

            if (_playerActorLastKnownSceneState.Count > LAST_KNOWN_SCENE_STATE_LIST_MAX_LENGTH)
            {
                _playerActorLastKnownSceneState.RemoveAt(0);
            }
        }

        public void Execute(ScenePostLoadingNotification notification)
        {
            int playerActorId = _playerActorManager.GetPlayerActorId();

            Pal3.Instance.Execute(new ActorActivateCommand(playerActorId, 1));

            if (_playerActorLastKnownSceneState.Count > 0 && _playerActorLastKnownSceneState.Any(_ =>
                        _.sceneInfo.ModelEquals(notification.NewSceneInfo)))
            {
                (ScnSceneInfo _, int actorNavIndex, Vector2Int actorTilePosition, Vector3 actorFacing) =
                    _playerActorLastKnownSceneState.Last(_ => _.sceneInfo.ModelEquals(notification.NewSceneInfo));

                Pal3.Instance.Execute(new ActorSetNavLayerCommand(playerActorId, actorNavIndex));
                Pal3.Instance.Execute(new ActorSetTilePositionCommand(playerActorId, actorTilePosition.x, actorTilePosition.y));

                _sceneManager.GetCurrentScene()
                    .GetActorGameEntity(playerActorId).Transform.Forward = actorFacing;
            }

            if (_playerActorManager.IsPlayerActorControlEnabled())
            {
                Pal3.Instance.Execute(new ActorEnablePlayerControlCommand(playerActorId));
            }

            if (_playerActorManager.IsPlayerInputEnabled())
            {
                Pal3.Instance.Execute(new PlayerEnableInputCommand(1));
            }

            #if PAL3
            Pal3.Instance.Execute(new LongKuiSwitchModeCommand(_longKuiLastKnownMode));
            #endif

            _isTilePositionPendingNotify = true;
        }

        public void Execute(ResetGameStateCommand command)
        {
            ResetAndDisposeJumpIndicators();

            _playerActorLastKnownSceneState.Clear();

            _currentMovementSfxAudioName = string.Empty;

            _lastInputTapPosition = null;
            _lastKnownPlayerActorAction = string.Empty;
            _isTilePositionPendingNotify = false;

            #if PAL3
            _longKuiLastKnownMode = 0;
            #endif

            _playerActor = null;
            _playerActorGameEntity = null;
            _playerActorController = null;
            _playerActorActionController = null;
            _playerActorMovementController = null;
        }

        #if PAL3
        public void Execute(LongKuiSwitchModeCommand command)
        {
            _longKuiLastKnownMode = command.Mode;
        }
        #endif
    }
}