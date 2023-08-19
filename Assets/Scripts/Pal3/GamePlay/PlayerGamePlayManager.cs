// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.GamePlay
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Actor;
    using Actor.Controllers;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataReader.Scn;
    using Core.Utils;
    using Data;
    using GameSystem;
    using Input;
    using MetaData;
    using Scene;
    using Scene.SceneObjects;
    using State;
    using UnityEngine;
    using UnityEngine.InputSystem;

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
        private readonly Camera _camera;

        private string _currentMovementSfxAudioName = string.Empty;

        private Vector2? _lastInputTapPosition;
        private Vector3? _lastKnownPosition;
        private Vector2Int? _lastKnownTilePosition;
        private int? _lastKnownLayerIndex;
        private bool _isTilePositionPendingNotify;

        private string _lastKnownPlayerActorAction = string.Empty;

        #if PAL3
        private int _longKuiLastKnownMode = 0;
        #endif

        private Actor _playerActor;
        private GameObject _playerActorGameObject;
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
            Camera mainCamera)
        {
            _resourceProvider = Requires.IsNotNull(resourceProvider, nameof(resourceProvider));
            _gameStateManager = Requires.IsNotNull(gameStateManager, nameof(gameStateManager));
            _playerActorManager = Requires.IsNotNull(playerActorManager, nameof(playerActorManager));
            _teamManager = Requires.IsNotNull(teamManager, nameof(teamManager));
            _inputActions = Requires.IsNotNull(inputActions, nameof(inputActions));
            _sceneManager = Requires.IsNotNull(sceneManager, nameof(sceneManager));
            _camera = Requires.IsNotNull(mainCamera, nameof(mainCamera));

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
            if (_playerActorGameObject == null) return;

            var isPlayerInControl = false;

            if (_gameStateManager.GetCurrentState() == GameState.Gameplay &&
                _playerActorManager.IsPlayerInputEnabled() &&
                _playerActorManager.IsPlayerActorControlEnabled())
            {
                isPlayerInControl = true;
                ReadInputAndMovePlayerIfNeeded();
            }

            var shouldUpdatePlayerActorMovementSfx = false;

            Vector3 position = _playerActorGameObject.transform.position;
            var layerIndex = _playerActorMovementController.GetCurrentLayerIndex();

            if (!(position == _lastKnownPosition && layerIndex == _lastKnownLayerIndex))
            {
                _lastKnownPosition = position;
                Vector2Int tilePosition = _playerActorMovementController.GetTilePosition();
                if (!(tilePosition == _lastKnownTilePosition && layerIndex == _lastKnownLayerIndex))
                {
                    shouldUpdatePlayerActorMovementSfx = true;
                    _lastKnownTilePosition = tilePosition;
                    PlayerActorTilePositionChanged(layerIndex, tilePosition, !isPlayerInControl);
                }
                else if (_isTilePositionPendingNotify)
                {
                    PlayerActorTilePositionChanged(layerIndex, tilePosition, !isPlayerInControl);
                    _isTilePositionPendingNotify = false;
                }
            }

            _lastKnownLayerIndex = layerIndex;

            var currentAction = _playerActorActionController.GetCurrentAction();
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
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new PlayerActorTilePositionUpdatedNotification(tilePosition, layerIndex, movedByScript));
        }

        public bool TryGetPlayerActorLastKnownPosition(out Vector3 position)
        {
            if (_lastKnownPosition.HasValue)
            {
                position = _lastKnownPosition.Value;
                return true;
            }

            position = Vector3.zero;
            return false;
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
            Transform actorTransform = _playerActorGameObject.transform;
            SceneObject sceneObject = _sceneManager.GetCurrentScene().GetSceneObject(command.SceneObjectId);

            if (sceneObject?.GetGameObject() is { } sceneObjectGameObject)
            {
                Vector3 objectPosition = sceneObjectGameObject.transform.position;

                actorTransform.LookAt(new Vector3(
                    objectPosition.x,
                    actorTransform.position.y,
                    objectPosition.z));
            }
        }

        public void Execute(ActorEnablePlayerControlCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID) return;

            // Check if actor is player actor.
            if (!Enum.IsDefined(typeof(PlayerActorId), command.ActorId))
            {
                Debug.LogError($"[{nameof(PlayerGamePlayManager)}] Cannot enable player control for actor " +
                               $"{command.ActorId} since actor is not a player actor.");
                return;
            }

            // Stop & dispose current player actor movement sfx
            if (_playerActorGameObject != null)
            {
                _currentMovementSfxAudioName = string.Empty;
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new StopSfxPlayingAtGameObjectRequest(_playerActorGameObject,
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
            if (_playerActorGameObject != null &&
                _playerActor.Info.Id != command.ActorId &&
                _playerActorController != null &&
                _playerActorController.IsActive)
            {
                lastActivePlayerActorNavLayerIndex = _playerActorMovementController.GetCurrentLayerIndex();
                _playerActorGameObject.transform.GetPositionAndRotation(out Vector3 currentPosition, out Quaternion currentRotation);
                lastActivePlayerActorPosition = currentPosition;
                lastActivePlayerActorRotation = currentRotation;
                CommandDispatcher<ICommand>.Instance.Dispatch(new ActorActivateCommand(_playerActor.Info.Id, 0));
            }

            // Set target actor as player actor
            _playerActor = _sceneManager.GetCurrentScene().GetActor(command.ActorId);
            _playerActorGameObject = _sceneManager.GetCurrentScene().GetActorGameObject(command.ActorId);
            _playerActorController = _playerActorGameObject.GetComponent<ActorController>();
            _playerActorActionController = _playerActorGameObject.GetComponent<ActorActionController>();
            _playerActorMovementController = _playerActorGameObject.GetComponent<ActorMovementController>();

            #if PAL3
            // LongKui should stay blue form when player control is enabled
            if (command.ActorId == (int)PlayerActorId.LongKui)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new LongKuiSwitchModeCommand(0));
            }
            #endif

            // Just to make sure the new actor is activated
            if (!_playerActorController.IsActive)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new ActorActivateCommand(_playerActor.Info.Id, 1));

                // Inherent nav layer index
                if (lastActivePlayerActorNavLayerIndex.HasValue)
                {
                    _playerActorMovementController.SetNavLayer(lastActivePlayerActorNavLayerIndex.Value);
                }
                // Inherent position
                if (lastActivePlayerActorPosition.HasValue)
                {
                    _playerActorGameObject.transform.position = lastActivePlayerActorPosition.Value;
                }
                // Inherent rotation
                if (lastActivePlayerActorRotation.HasValue)
                {
                    _playerActorGameObject.transform.rotation = lastActivePlayerActorRotation.Value;
                }
            }

            // Reset states
            _lastKnownPosition = null;
            _lastKnownTilePosition = null;
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

                _lastKnownPosition = null;
                _lastKnownTilePosition = null;
            }
        }

        public void Execute(ActorSetTilePositionCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID ||
                (_playerActor != null && _playerActor.Info.Id == command.ActorId))
            {
                _isTilePositionPendingNotify = true;
            }
        }

        public void Execute(SceneLeavingCurrentSceneNotification command)
        {
            if (_sceneManager.GetCurrentScene() is not { } currentScene) return;

            // Stop current player actor movement sfx
            if (_playerActorGameObject != null)
            {
                _currentMovementSfxAudioName = string.Empty;
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new StopSfxPlayingAtGameObjectRequest(_playerActorGameObject,
                        AudioConstants.PlayerActorMovementSfxAudioSourceName,
                        disposeSource: true));
            }

            // Remove game play indicators
            ResetAndDisposeJumpIndicators();

            _playerActorLastKnownSceneState.Add((
                currentScene.GetSceneInfo(),
                _playerActorMovementController.GetCurrentLayerIndex(),
                _playerActorMovementController.GetTilePosition(),
                _playerActorGameObject.transform.forward));

            if (_playerActorLastKnownSceneState.Count > LAST_KNOWN_SCENE_STATE_LIST_MAX_LENGTH)
            {
                _playerActorLastKnownSceneState.RemoveAt(0);
            }
        }

        public void Execute(ScenePostLoadingNotification notification)
        {
            var playerActorId = (int)_playerActorManager.GetPlayerActor();

            CommandDispatcher<ICommand>.Instance.Dispatch(new ActorActivateCommand(playerActorId, 1));

            if (_playerActorLastKnownSceneState.Count > 0 && _playerActorLastKnownSceneState.Any(_ =>
                        _.sceneInfo.ModelEquals(notification.NewSceneInfo)))
            {
                (ScnSceneInfo _, int actorNavIndex, Vector2Int actorTilePosition, Vector3 actorFacing) =
                    _playerActorLastKnownSceneState.Last(_ => _.sceneInfo.ModelEquals(notification.NewSceneInfo));

                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorSetNavLayerCommand(playerActorId, actorNavIndex));
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorSetTilePositionCommand(playerActorId, actorTilePosition.x, actorTilePosition.y));

                _sceneManager.GetCurrentScene()
                    .GetActorGameObject(playerActorId).transform.forward = actorFacing;
            }

            if (_playerActorManager.IsPlayerActorControlEnabled())
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new ActorEnablePlayerControlCommand(playerActorId));
            }

            if (_playerActorManager.IsPlayerInputEnabled())
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new PlayerEnableInputCommand(1));
            }

            #if PAL3
            CommandDispatcher<ICommand>.Instance.Dispatch(new LongKuiSwitchModeCommand(_longKuiLastKnownMode));
            #endif

            _isTilePositionPendingNotify = true;
        }

        public void Execute(ResetGameStateCommand command)
        {
            ResetAndDisposeJumpIndicators();

            _playerActorLastKnownSceneState.Clear();

            _currentMovementSfxAudioName = string.Empty;

            _lastInputTapPosition = null;
            _lastKnownPosition = null;
            _lastKnownTilePosition = null;
            _lastKnownLayerIndex = null;
            _lastKnownPlayerActorAction = string.Empty;
            _isTilePositionPendingNotify = false;

            #if PAL3
            _longKuiLastKnownMode = 0;
            #endif

            _playerActor = null;
            _playerActorGameObject = null;
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