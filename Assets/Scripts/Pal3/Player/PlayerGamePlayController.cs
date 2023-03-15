// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Player
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Actor;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.Animation;
    using Core.DataReader.Nav;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Core.Renderer;
    using Data;
    using Input;
    using MetaData;
    using Scene;
    using Scene.SceneObjects;
    using Scene.SceneObjects.Common;
    using Script;
    using Script.Waiter;
    using State;
    using UnityEngine;
    using UnityEngine.InputSystem;

    public sealed class PlayerGamePlayController : MonoBehaviour,
        ICommandExecutor<ActorEnablePlayerControlCommand>,
        ICommandExecutor<ActorSetTilePositionCommand>,
        ICommandExecutor<PlayerEnableInputCommand>,
        ICommandExecutor<ActorPerformClimbActionCommand>,
        ICommandExecutor<PlayerInteractionRequest>,
        #if PAL3
        ICommandExecutor<LongKuiSwitchModeCommand>,
        #endif
        ICommandExecutor<SceneLeavingCurrentSceneNotification>,
        ICommandExecutor<ScenePostLoadingNotification>,
        ICommandExecutor<PlayerActorLookAtSceneObjectCommand>,
        ICommandExecutor<PlayerInteractWithObjectCommand>,
        ICommandExecutor<ToggleBigMapRequest>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private const float MIN_JUMP_DISTANCE = 1.5f;
        private const float MAX_JUMP_DISTANCE = 8f;
        private const float MAX_JUMP_Y_DIFFERENTIAL = 3.5f;
        private const float JUMP_HEIGHT = 6f;

        private GameResourceProvider _resourceProvider;
        private GameStateManager _gameStateManager;
        private PlayerManager _playerManager;
        private TeamManager _teamManager;
        private PlayerInputActions _inputActions;
        private SceneManager _sceneManager;
        private Camera _camera;

        private string _currentMovementSfxAudioName = string.Empty;
        private const float PLAYER_ACTOR_MOVEMENT_SFX_WALK_VOLUME = 0.6f;
        private const float PLAYER_ACTOR_MOVEMENT_SFX_RUN_VOLUME = 1.0f;

        private Vector2? _lastInputTapPosition;
        private Vector3? _lastKnownPosition;
        private Vector2Int? _lastKnownTilePosition;
        private int? _lastKnownLayerIndex;
        private string _lastKnownPlayerActorAction = string.Empty;
        private int _jumpableAreaEnterCount;
        #if PAL3
        private int _longKuiLastKnownMode = 0;
        #endif

        private Actor _playerActor;
        private GameObject _playerActorGameObject;
        private ActorController _playerActorController;
        private ActorActionController _playerActorActionController;
        private ActorMovementController _playerActorMovementController;

        private GameObject _jumpIndicatorGameObject;
        private AnimatedBillboardRenderer _jumpIndicatorRenderer;

        private const int LAST_KNOWN_SCENE_STATE_LIST_MAX_LENGTH = 2;
        private readonly List<(ScnSceneInfo sceneInfo,
            int actorNavIndex,
            Vector2Int actorTilePosition,
            Vector3 actorFacing)> _playerActorLastKnownSceneState = new ();

        public void Init(GameResourceProvider resourceProvider,
            GameStateManager gameStateManager,
            PlayerManager playerManager,
            TeamManager teamManager,
            PlayerInputActions inputActions,
            SceneManager sceneManager,
            Camera mainCamera)
        {
            _resourceProvider = resourceProvider ?? throw new ArgumentNullException(nameof(resourceProvider));
            _gameStateManager = gameStateManager ?? throw new ArgumentNullException(nameof(gameStateManager));
            _playerManager = playerManager ?? throw new ArgumentNullException(nameof(playerManager));
            _teamManager = teamManager ?? throw new ArgumentNullException(nameof(teamManager));
            _inputActions = inputActions ?? throw new ArgumentNullException(nameof(inputActions));
            _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
            _camera = mainCamera != null ? mainCamera : throw new ArgumentNullException(nameof(mainCamera));

            _inputActions.Gameplay.OnTap.performed += OnTapPerformed;
            _inputActions.Gameplay.OnMove.performed += OnMovePerformed;
            _inputActions.Gameplay.OnDoubleTap.performed += OnDoubleTapPerformed;
            _inputActions.Gameplay.PortalTo.performed += PortalToPerformed;
            _inputActions.Gameplay.Interact.performed += InteractionPerformed;
            _inputActions.Gameplay.Movement.canceled += MovementCanceled;
            _inputActions.Gameplay.SwitchToPreviousPlayerActor.performed += SwitchToPreviousPlayerActorPerformed;
            _inputActions.Gameplay.SwitchToNextPlayerActor.performed += SwitchToNextPlayerActorPerformed;
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
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

        private void Update()
        {
            if (_playerActorGameObject == null) return;

            var isPlayerInControl = false;

            if (_gameStateManager.GetCurrentState() == GameState.Gameplay &&
                _playerManager.IsPlayerInputEnabled() &&
                _playerManager.IsPlayerActorControlEnabled())
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

        private string GetMovementSfxName(ActorActionType movementAction)
        {
            if (movementAction is not (ActorActionType.Walk or ActorActionType.Run)) return string.Empty;

            #if PAL3
            var sfxPrefix = movementAction == ActorActionType.Walk ? "we021" : "we022";

            Tilemap tileMap = _sceneManager.GetCurrentScene().GetTilemap();
            if ((_lastKnownPosition.HasValue && _lastKnownLayerIndex.HasValue) &&
                tileMap.TryGetTile(_lastKnownPosition.Value, _lastKnownLayerIndex.Value, out NavTile tile))
            {
                return tile.FloorKind switch
                {
                    NavFloorKind.Grass => sfxPrefix + 'b',
                    NavFloorKind.Snow => sfxPrefix + 'c',
                    NavFloorKind.Sand => sfxPrefix + 'd',
                    _ => sfxPrefix + 'a'
                };
            }
            else
            {
                return sfxPrefix + 'a';
            }
            #elif PAL3A
            if (_playerActor.Info.Id == (byte)PlayerActorId.WangPengXu)
            {
                return movementAction == ActorActionType.Walk ? "WE007" : "WE045";
            }
            return movementAction == ActorActionType.Walk ? "WE007" : "WE008";
            #endif
        }

        private void UpdatePlayerActorMovementSfx(string playerActorAction)
        {
            var newMovementSfxAudioFileName = string.Empty;
            ActorActionType actionType = ActorActionType.Walk;

            if (string.Equals(playerActorAction, ActorConstants.ActionNames[ActorActionType.Walk]))
            {
                newMovementSfxAudioFileName = GetMovementSfxName(ActorActionType.Walk);
                actionType = ActorActionType.Walk;
            }
            else if (string.Equals(playerActorAction, ActorConstants.ActionNames[ActorActionType.Run]))
            {
                newMovementSfxAudioFileName = GetMovementSfxName(ActorActionType.Run);
                actionType = ActorActionType.Run;
            }

            if (string.Equals(_currentMovementSfxAudioName, newMovementSfxAudioFileName)) return;

            _currentMovementSfxAudioName = newMovementSfxAudioFileName;

            if (string.IsNullOrEmpty(_currentMovementSfxAudioName))
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new StopSfxPlayingAtGameObjectRequest(_playerActorGameObject,
                        AudioConstants.PlayerActorMovementSfxAudioSourceName,
                        disposeSource: false));
            }
            else
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new AttachSfxToGameObjectRequest(_playerActorGameObject,
                        newMovementSfxAudioFileName,
                        AudioConstants.PlayerActorMovementSfxAudioSourceName,
                        loopCount: -1,
                        actionType == ActorActionType.Walk
                            ? PLAYER_ACTOR_MOVEMENT_SFX_WALK_VOLUME
                            : PLAYER_ACTOR_MOVEMENT_SFX_RUN_VOLUME,
                        startDelayInSeconds: 0f,
                        interval: 0f));
            }
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

        private void ReadInputAndMovePlayerIfNeeded()
        {
            var movement = _inputActions.Gameplay.Movement.ReadValue<Vector2>();
            if (movement == Vector2.zero) return;

            MovementMode movementMode = movement.magnitude < 0.7f ? MovementMode.Walk : MovementMode.Run;
            ActorActionType movementAction = movementMode == MovementMode.Walk ? ActorActionType.Walk : ActorActionType.Run;
            _playerActorMovementController.CancelMovement();
            Transform cameraTransform = _camera.transform;
            Vector3 inputDirection = cameraTransform.forward * movement.y +
                                     cameraTransform.right * movement.x;
            MovementResult result = PlayerActorMoveTowards(inputDirection, movementMode);
            _playerActorActionController.PerformAction(result == MovementResult.Blocked
                ? ActorActionType.Stand
                : movementAction);
        }

        /// <summary>
        /// Adjust some degrees to the inputDirection to prevent player actor
        /// from hitting into the wall and gets blocked.
        /// This process is purely for improving the gameplay experience.
        /// </summary>
        /// <param name="inputDirection">User input direction in game space</param>
        /// <param name="movementMode">Player actor movement mode</param>
        /// <returns>MovementResult</returns>
        private MovementResult PlayerActorMoveTowards(Vector3 inputDirection, MovementMode movementMode)
        {
            Vector3 playerActorPosition = _playerActorGameObject.transform.position;
            MovementResult result = _playerActorMovementController.MoveTowards(
                playerActorPosition + inputDirection, movementMode);

            if (result != MovementResult.Blocked) return result;

            if (_playerActorMovementController.IsDuringCollision()) return result;

            // Don't adjust direction if player actor is inside jumpable area
            if (IsPlayerActorInsideJumpableArea()) return result;

            // Try change direction a little bit to see if it works
            for (var degrees = 2; degrees <= 80; degrees+= 2)
            {
                // + degrees
                {
                    Vector3 newDirection = Quaternion.Euler(0f, degrees, 0f) * inputDirection;
                    result = _playerActorMovementController.MoveTowards(
                        playerActorPosition + newDirection, movementMode);
                    if (result != MovementResult.Blocked) return result;
                }
                // - degrees
                {
                    Vector3 newDirection = Quaternion.Euler(0f, -degrees, 0f) * inputDirection;
                    result = _playerActorMovementController.MoveTowards(
                        playerActorPosition + newDirection, movementMode);
                    if (result != MovementResult.Blocked) return result;
                }
            }

            return result;
        }

        private void MovementCanceled(InputAction.CallbackContext _)
        {
            _playerActorActionController.PerformAction(ActorActionType.Stand);
        }

        private void OnTapPerformed(InputAction.CallbackContext _)
        {
            if (!_playerManager.IsPlayerActorControlEnabled() ||
                !_playerManager.IsPlayerInputEnabled())
            {
                return;
            }

            MoveToTapPosition(false);
        }

        private void OnDoubleTapPerformed(InputAction.CallbackContext _)
        {
            if (!_playerManager.IsPlayerActorControlEnabled() ||
                !_playerManager.IsPlayerInputEnabled())
            {
                return;
            }

            MoveToTapPosition(true);
        }

        private void PortalToPerformed(InputAction.CallbackContext _)
        {
            if (!_playerManager.IsPlayerActorControlEnabled() ||
                !_playerManager.IsPlayerInputEnabled())
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
            if (!_playerManager.IsPlayerActorControlEnabled() ||
                !_playerManager.IsPlayerInputEnabled())
            {
                return;
            }

            _lastInputTapPosition = ctx.ReadValue<Vector2>();
        }

        private void InteractionPerformed(InputAction.CallbackContext _)
        {
            if (!_playerManager.IsPlayerActorControlEnabled() ||
                !_playerManager.IsPlayerInputEnabled())
            {
                return;
            }

            if (IsPlayerActorInsideJumpableArea())
            {
                StartCoroutine(JumpAsync());
            }
            else
            {
                InteractWithFacingInteractable();
            }
        }

        private IEnumerator JumpAsync(Vector3? jumpTargetPosition = null)
        {
            _gameStateManager.GoToState(GameState.Cutscene);
            _playerActorMovementController.CancelMovement();

            Vector3 currentPosition = _playerActorMovementController.GetWorldPosition();

            Scene currentScene = _sceneManager.GetCurrentScene();
            Tilemap tilemap = currentScene.GetTilemap();

            bool IsPositionCanJumpTo(Vector3 position, int layerIndex, out float yPosition, out int distanceToObstacle)
            {
                Vector2Int tilePosition = tilemap.GetTilePosition(position, layerIndex);

                if (tilemap.TryGetTile(position, layerIndex, out NavTile tile) &&
                    tile.IsWalkable())
                {
                    distanceToObstacle = tile.DistanceToNearestObstacle;
                    yPosition = GameBoxInterpreter.ToUnityYPosition(tile.GameBoxYPosition);

                    if (Mathf.Abs(yPosition - currentPosition.y) > MAX_JUMP_Y_DIFFERENTIAL) return false;

                    if (currentScene.IsPositionInsideJumpableArea(layerIndex, tilePosition))
                    {
                        return true;
                    }
                }

                yPosition = 0f;
                distanceToObstacle = 0;
                return false;
            }

            Vector3 jumpDirection = _playerActorGameObject.transform.forward;

            if (jumpTargetPosition != null)
            {
                jumpDirection = jumpTargetPosition.Value - _playerActorGameObject.transform.position;
                jumpDirection.y = 0f;
                jumpDirection.Normalize();
                _playerActorGameObject.transform.forward = jumpDirection;
            }

            var validJumpTargetPositions = new List<(Vector3 position, int layerIndex, int distanceToObstacle)>();

            for (float i = MIN_JUMP_DISTANCE; i <= MAX_JUMP_DISTANCE; i += 0.15f)
            {
                Vector3 targetPosition = currentPosition + jumpDirection * i;

                for (var j = 0; j < tilemap.GetLayerCount(); j++)
                {
                    if (IsPositionCanJumpTo(targetPosition, j,
                            out float yPosition, out int distanceToObstacle))
                    {
                        Vector3 position = targetPosition;
                        position.y = yPosition;
                        validJumpTargetPositions.Add((position, j, distanceToObstacle));
                    }
                }
            }

            var currentLayer = _playerActorMovementController.GetCurrentLayerIndex();
            var jumpTargetLayer = currentLayer;
            if (validJumpTargetPositions.Count > 0)
            {
                if (validJumpTargetPositions.Any(_ => _.layerIndex != currentLayer))
                {
                    validJumpTargetPositions = validJumpTargetPositions.Where(_ => _.layerIndex != currentLayer).ToList();
                }

                validJumpTargetPositions.Sort((a, b) => b.distanceToObstacle.CompareTo(a.distanceToObstacle));

                // Pick a position that is farthest from obstacles
                var bestPosition = validJumpTargetPositions.First();
                jumpTargetPosition = bestPosition.position;
                jumpTargetLayer = bestPosition.layerIndex;
            }
            else
            {
                jumpTargetPosition = currentPosition;
            }

            _playerActorActionController.PerformAction(ActorConstants.ActionNames[ActorActionType.Jump],
                 overwrite: true, loopCount: 1);
            yield return new WaitForSeconds(0.7f);

            var xzOffset = Vector2.Distance(
                new Vector2(jumpTargetPosition.Value.x, jumpTargetPosition.Value.z),
                new Vector2(currentPosition.x, currentPosition.z));
            var startingYPosition = currentPosition.y;
            var yOffset = jumpTargetPosition.Value.y - currentPosition.y;

            yield return AnimationHelper.EnumerateValueAsync(0f, 1f, 1.1f, AnimationCurveType.Sine,
                value =>
                {
                    Vector3 calculatedPosition = currentPosition + jumpDirection * (xzOffset * value);
                    calculatedPosition.y = startingYPosition + (0.5f - MathF.Abs(value - 0.5f)) * JUMP_HEIGHT + yOffset * value;
                    _playerActorGameObject.transform.position = calculatedPosition;
                });
            yield return new WaitForSeconds(0.7f);

            _playerActorMovementController.SetNavLayer(jumpTargetLayer);

            PlayerActorTilePositionChanged(jumpTargetLayer,
                tilemap.GetTilePosition(jumpTargetPosition.Value,
                    jumpTargetLayer),
                false);

            _gameStateManager.GoToState(GameState.Gameplay);
        }

        private void SwitchToNextPlayerActorPerformed(InputAction.CallbackContext _)
        {
            SwitchPlayerActorInCurrentTeam(true);
        }

        private void SwitchToPreviousPlayerActorPerformed(InputAction.CallbackContext _)
        {
            SwitchPlayerActorInCurrentTeam(false);
        }

        private void SwitchPlayerActorInCurrentTeam(bool next)
        {
            if (!_playerManager.IsPlayerActorControlEnabled() ||
                !_playerManager.IsPlayerInputEnabled() ||
                _sceneManager.GetCurrentScene().GetSceneInfo().SceneType != ScnSceneType.Maze)
            {
                return;
            }

            var actorsInTeam = _teamManager.GetActorsInTeam();
            if (actorsInTeam.Count <= 1) return; // Makes no sense to change player actor if there is only one actor in the team

            var playerActorIdLength = Enum.GetNames(typeof(PlayerActorId)).Length;
            int targetPlayerActorId = _playerActor.Info.Id;

            do
            {
                targetPlayerActorId = (targetPlayerActorId + playerActorIdLength + (next ? +1 : -1)) % playerActorIdLength;
            } while (!actorsInTeam.Contains((PlayerActorId) targetPlayerActorId));

            CommandDispatcher<ICommand>.Instance.Dispatch(new ActorEnablePlayerControlCommand(targetPlayerActorId));
        }

        /// <summary>
        /// Interact with the nearby interactable object by the player actor facing direction.
        /// </summary>
        private void InteractWithFacingInteractable()
        {
            Vector3 actorFacingDirection = _playerActorMovementController.transform.forward;
            Vector3 actorCenterPosition = _playerActorActionController.GetRendererBounds().center;
            var currentLayerIndex = _playerActorMovementController.GetCurrentLayerIndex();

            float nearestInteractableFacingAngle = 181f;
            IEnumerator interactionRoutine = null;

            foreach (var sceneObjectId in
                     _sceneManager.GetCurrentScene().GetAllActivatedSceneObjects())
            {
                SceneObject sceneObject = _sceneManager.GetCurrentScene().GetSceneObject(sceneObjectId);

                Vector3 closetPointOnObject = sceneObject.GetRendererBounds().ClosestPoint(actorCenterPosition);
                float distanceToActor = Vector3.Distance(actorCenterPosition, closetPointOnObject);
                Vector3 actorToObjectFacing = closetPointOnObject - actorCenterPosition;
                float facingAngle = Vector2.Angle(
                    new Vector2(actorFacingDirection.x, actorFacingDirection.z),
                    new Vector2(actorToObjectFacing.x, actorToObjectFacing.z));

                if (sceneObject.IsDirectlyInteractable(distanceToActor) &&
                    facingAngle < nearestInteractableFacingAngle)
                {
                    //Debug.DrawLine(actorCenterPosition, closetPointOnObject, Color.white, 1000);
                    nearestInteractableFacingAngle = facingAngle;
                    interactionRoutine = InteractWithSceneObjectAsync(sceneObject, startedByPlayer: true);
                }
            }

            foreach (var actorInfo in
                     _sceneManager.GetCurrentScene().GetAllActorGameObjects())
            {
                var actorController = actorInfo.Value.GetComponent<ActorController>();
                var actorActionController = actorInfo.Value.GetComponent<ActorActionController>();
                var actorMovementController = actorInfo.Value.GetComponent<ActorMovementController>();

                if (actorMovementController.GetCurrentLayerIndex() != currentLayerIndex ||
                    actorInfo.Key == (int)_playerManager.GetPlayerActor() ||
                    !actorController.IsActive) continue;

                Vector3 targetActorCenterPosition = actorActionController.GetRendererBounds().center;
                var distance = Vector3.Distance(actorCenterPosition,targetActorCenterPosition);
                Vector3 actorToActorFacing = targetActorCenterPosition - actorCenterPosition;
                float facingAngle = Vector2.Angle(
                    new Vector2(actorFacingDirection.x, actorFacingDirection.z),
                    new Vector2(actorToActorFacing.x, actorToActorFacing.z));

                if (actorController.IsDirectlyInteractable(distance) &&
                    facingAngle < nearestInteractableFacingAngle)
                {
                    //Debug.DrawLine(actorCenterPosition, targetActorCenterPosition, Color.white, 1000);
                    nearestInteractableFacingAngle = facingAngle;
                    interactionRoutine = InteractWithActorAsync(actorInfo.Key, actorInfo.Value);
                }
            }

            if (interactionRoutine != null)
            {
                StartCoroutine(interactionRoutine);
            }
        }

        private IEnumerator InteractWithSceneObjectAsync(SceneObject sceneObject, bool startedByPlayer)
        {
            var correlationId = Guid.NewGuid();
            var requiresStateChange = sceneObject.ShouldGoToCutsceneWhenInteractionStarted();

            if (requiresStateChange)
            {
                _gameStateManager.GoToState(GameState.Cutscene);
                _gameStateManager.AddGamePlayStateLocker(correlationId);
            }

            yield return sceneObject.InteractAsync(new InteractionContext
            {
                CorrelationId = correlationId,
                InitObjectId = sceneObject.ObjectInfo.Id,
                PlayerActorGameObject = _playerActorGameObject,
                CurrentScene = _sceneManager.GetCurrentScene(),
                StartedByPlayer = startedByPlayer,
            });

            if (requiresStateChange)
            {
                _gameStateManager.RemoveGamePlayStateLocker(correlationId);
                _gameStateManager.GoToState(GameState.Gameplay);
            }
        }

        private IEnumerator InteractWithActorAsync(int actorId, GameObject actorGameObject)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new GameStateChangeRequest(GameState.Cutscene));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));

            Actor targetActor = _sceneManager.GetCurrentScene().GetActor(actorId);
            Quaternion rotationBeforeInteraction = actorGameObject.transform.rotation;

            var actorController = actorGameObject.GetComponent<ActorController>();
            var movementController = actorGameObject.GetComponent<ActorMovementController>();

            // Pause current path follow movement of the interacting actor
            if (actorController != null &&
                movementController != null &&
                actorController.GetCurrentBehaviour() == ScnActorBehaviour.PathFollow)
            {
                movementController.PauseMovement();
            }

            // Look at the target actor
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorLookAtActorCommand(_playerActor.Info.Id, targetActor.Info.Id));

            // Only let target actor look at player actor when the target actor is in idle state
            if (actorGameObject.GetComponent<ActorActionController>() is { } actionController &&
                actionController.IsCurrentActionIdleAction())
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorLookAtActorCommand(targetActor.Info.Id, _playerActor.Info.Id));
            }

            // Run dialogue script
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ScriptRunCommand((int)targetActor.Info.ScriptId));

            // Wait until the dialogue script is finished
            yield return new WaitUntilScriptFinished(PalScriptType.Scene, targetActor.Info.ScriptId);

            // Resume current path follow movement of the interacting actor
            if (actorController != null &&
                movementController != null &&
                actorController.GetCurrentBehaviour() == ScnActorBehaviour.PathFollow)
            {
                movementController.ResumeMovement();
            }

            // Reset facing rotation of the interacting actor
            if (actorGameObject != null)
            {
                actorGameObject.transform.rotation = rotationBeforeInteraction;
            }
        }

        private void PortalToTapPosition()
        {
            if (!_lastInputTapPosition.HasValue) return;

            Ray ray = _camera.ScreenPointToRay(_lastInputTapPosition.Value);

            if (!Physics.Raycast(ray, out RaycastHit hit)) return;

            int layerIndex;
            bool isPositionOnStandingPlatform;

            if (hit.collider.gameObject.GetComponent<StandingPlatformController>() is { } standingPlatformController)
            {
                layerIndex = standingPlatformController.LayerIndex;
                isPositionOnStandingPlatform = true;
            }
            else
            {
                var meshColliders = _sceneManager.GetCurrentScene()
                    .GetMeshColliders();

                if (meshColliders.Any(_ => _.Value == hit.collider))
                {
                    layerIndex = meshColliders.First(_ => _.Value == hit.collider).Key;
                    isPositionOnStandingPlatform = false;
                }
                else
                {
                    // Raycast hit a collider that is not a mesh collider or a standing platform
                    return;
                }
            }

            _playerActorMovementController.PortalToPosition(hit.point, layerIndex, isPositionOnStandingPlatform);
        }

        private void JumpToTapPosition()
        {
            if (!_lastInputTapPosition.HasValue) return;

            Ray ray = _camera.ScreenPointToRay(_lastInputTapPosition.Value);

            if (!Physics.Raycast(ray, out RaycastHit hit)) return;

            if (_sceneManager.GetCurrentScene()
                    .GetMeshColliders().Any(_ => _.Value == hit.collider))
            {
                StartCoroutine(JumpAsync(hit.point));
            }
        }

        // Raycast caches to avoid GC
        private readonly RaycastHit[] _raycastHits = new RaycastHit[4];
        private readonly Dictionary<int, (Vector3 point, bool isPlatform)> _tapPoints = new ();
        private void MoveToTapPosition(bool isDoubleTap)
        {
            if (!_lastInputTapPosition.HasValue) return;

            Scene currentScene = _sceneManager.GetCurrentScene();
            var meshColliders = currentScene.GetMeshColliders();

            Ray ray = _camera.ScreenPointToRay(_lastInputTapPosition.Value);

            var hitCount = Physics.RaycastNonAlloc(ray, _raycastHits, 500f);
            if (hitCount == 0) return;

            Tilemap tilemap = currentScene.GetTilemap();

            _tapPoints.Clear();
            for (var i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _raycastHits[i];

                if (hit.collider.gameObject.GetComponent<StandingPlatformController>() is { }
                        standingPlatformController)
                {
                    _tapPoints[standingPlatformController.LayerIndex] = (hit.point, true);
                    continue;
                }

                var layerIndex = meshColliders.FirstOrDefault(_ => _.Value == hit.collider).Key;

                if (!tilemap.TryGetTile(hit.point, layerIndex, out NavTile _))
                {
                    continue;
                }

                Vector3 cameraPosition = _camera.transform.position;
                var distanceToCamera = Vector3.Distance(cameraPosition, hit.point);

                if (_tapPoints.ContainsKey(layerIndex))
                {
                    var existingDistance = Vector3.Distance(cameraPosition, _tapPoints[layerIndex].point);
                    if (distanceToCamera < existingDistance)
                    {
                        _tapPoints[layerIndex] = (hit.point, false);
                    }
                }
                else
                {
                    _tapPoints[layerIndex] = (hit.point, false);
                }
            }

            if (_tapPoints.Count > 0)
            {
                _playerActorMovementController.MoveToTapPoint(_tapPoints, isDoubleTap);
            }
        }

        public void PlayerActorEnteredJumpableArea()
        {
            if (_jumpIndicatorGameObject == null)
            {
                var sprites = _resourceProvider.GetJumpIndicatorSprites();

                _jumpIndicatorGameObject = new GameObject($"JumpIndicator");
                _jumpIndicatorGameObject.transform.SetParent(_playerActorGameObject.transform, false);
                _jumpIndicatorGameObject.transform.localScale = new Vector3(3f, 3f, 3f);
                _jumpIndicatorGameObject.transform.localPosition = new Vector3(0f,
                    _playerActorActionController.GetActorHeight() + 1f, 0f);

                _jumpIndicatorRenderer = _jumpIndicatorGameObject.AddComponent<AnimatedBillboardRenderer>();
                _jumpIndicatorRenderer.Init(sprites, 4);
            }

            if (_jumpableAreaEnterCount == 0)
            {
                _jumpIndicatorRenderer.StartAnimation(-1);
            }

            _jumpableAreaEnterCount++;
        }

        public void PlayerActorExitedJumpableArea()
        {
            _jumpableAreaEnterCount--;

            if (_jumpableAreaEnterCount == 0 && _jumpIndicatorRenderer != null)
            {
                _jumpIndicatorRenderer.StopAnimation();
            }
        }

        private bool IsPlayerActorInsideJumpableArea()
        {
            return _jumpableAreaEnterCount > 0;
        }

        public void Execute(PlayerInteractWithObjectCommand command)
        {
            Scene currentScene = _sceneManager.GetCurrentScene();
            if (currentScene.GetAllActivatedSceneObjects().Contains(command.SceneObjectId))
            {
                StartCoroutine(InteractWithSceneObjectAsync(
                    currentScene.GetSceneObject(command.SceneObjectId), startedByPlayer: false));
            }
            else
            {
                Debug.LogError($"Scene object not found or not activated yet: {command.SceneObjectId}.");
            }
        }

        public void Execute(ActorPerformClimbActionCommand command)
        {
            Scene scene = _sceneManager.GetCurrentScene();
            SceneObject climbableSceneObject = scene.GetSceneObject(command.ObjectId);
            GameObject climbableObject = climbableSceneObject.GetGameObject();
            if (climbableObject == null)
            {
                Debug.LogError($"Scene object not found or not activated yet: {command.ObjectId}.");
                return;
            }

            Vector3 climbableObjectPosition = climbableObject.transform.position;
            Vector3 climbableObjectFacing = GameBoxInterpreter.ToUnityRotation(
                new Vector3(0f, climbableSceneObject.ObjectInfo.GameBoxYRotation, 0f)) * Vector3.forward;

            Vector3 lowerPosition = climbableObjectFacing.normalized * 0.5f + climbableObjectPosition;
            Vector3 lowerStandingPosition = climbableObjectFacing.normalized * 1.5f + climbableObjectPosition;
            Vector3 upperPosition = -climbableObjectFacing.normalized * 0.5f + climbableObjectPosition;
            Vector3 upperStandingPosition = -climbableObjectFacing.normalized * 1.5f + climbableObjectPosition;

            var currentPlayerLayer = _playerActorMovementController.GetCurrentLayerIndex();

            var climbUp = (command.ClimbUp == 1);
            var climbableHeight = climbableObject.GetComponentInChildren<StaticMeshRenderer>()
                                      .GetRendererBounds().max.y / 2f; // Half is enough for the animation

            Vector3 playerCurrentPosition = _playerActorGameObject.transform.position;
            if (command.ClimbUp == 1)
            {
                lowerPosition.y = playerCurrentPosition.y;
                lowerStandingPosition.y = playerCurrentPosition.y;
                upperPosition.y = playerCurrentPosition.y + climbableHeight;
                upperStandingPosition.y = playerCurrentPosition.y + climbableHeight;
            }
            else
            {
                lowerPosition.y = playerCurrentPosition.y - climbableHeight;
                lowerStandingPosition.y = playerCurrentPosition.y - climbableHeight;
                upperPosition.y = playerCurrentPosition.y;
                upperStandingPosition.y = playerCurrentPosition.y;
            }

            var waiter = new WaitUntilCanceled();
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(waiter));

            var climbAnimationOnly = command.ClimbUp != -1;
            StartCoroutine(PlayerActorMoveToClimbableObjectAndClimbAsync(climbableObject, climbUp,
                climbAnimationOnly, climbableHeight, lowerPosition, lowerStandingPosition,
                upperPosition, upperStandingPosition, currentPlayerLayer, currentPlayerLayer, () =>
                {
                    waiter.CancelWait();
                }));
        }

        public IEnumerator PlayerActorMoveToClimbableObjectAndClimbAsync(
            GameObject climbableObject,
            bool climbUp,
            bool climbOnly,
            float climbableHeight,
            Vector3 lowerPosition,
            Vector3 lowerStandingPosition,
            Vector3 upperPosition,
            Vector3 upperStandingPosition,
            int lowerLayer,
            int upperLayer,
            Action onFinished = null)
        {
            yield return _playerActorMovementController
                .MoveDirectlyToAsync(climbUp ? lowerPosition : upperPosition, 0, true);

            _playerActorActionController.PerformAction(climbUp ? ActorActionType.Climb : ActorActionType.ClimbDown);

            _playerActorGameObject.transform.position = new Vector3(lowerPosition.x,
                _playerActorGameObject.transform.position.y, lowerPosition.z);

            var objectRotationY = climbableObject.transform.rotation.eulerAngles.y;
            _playerActorGameObject.transform.rotation = Quaternion.Euler(0f, objectRotationY + 180f, 0f);

            if (climbUp)
            {
                var currentHeight = 0f;
                while (currentHeight < climbableHeight)
                {
                    var delta = Time.deltaTime * ActorConstants.ActorClimbSpeed;
                    currentHeight += delta;
                    _playerActorGameObject.transform.position += new Vector3(0f, delta, 0f);
                    yield return null;
                }

                if (!climbOnly)
                {
                    _playerActorMovementController.SetNavLayer(upperLayer);
                    yield return _playerActorMovementController.MoveDirectlyToAsync(upperStandingPosition, 0, true);
                    _playerActorGameObject.transform.position = upperStandingPosition;
                }
            }
            else
            {
                var currentHeight = climbableHeight;
                while (currentHeight > 0f)
                {
                    var delta = Time.deltaTime * ActorConstants.ActorClimbSpeed;
                    currentHeight -= delta;
                    _playerActorGameObject.transform.position -= new Vector3(0f, delta, 0f);
                    yield return null;
                }

                if (!climbOnly)
                {
                    _playerActorMovementController.SetNavLayer(lowerLayer);
                    yield return _playerActorMovementController.MoveDirectlyToAsync(lowerStandingPosition, 0, true);
                    _playerActorGameObject.transform.position = lowerStandingPosition;
                }
            }

            onFinished?.Invoke();
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
                Debug.LogError($"Cannot enable player control for actor {command.ActorId} " +
                               $"since actor is not player actor.");
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

            // Dispose current game play indicator
            if (_jumpIndicatorGameObject != null)
            {
                Destroy(_jumpIndicatorGameObject);
                _jumpIndicatorGameObject = null;
                _jumpIndicatorRenderer = null;
            }

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
                lastActivePlayerActorPosition = _playerActorGameObject.transform.position;
                lastActivePlayerActorRotation = _playerActorGameObject.transform.rotation;
                CommandDispatcher<ICommand>.Instance.Dispatch(new ActorActivateCommand(_playerActor.Info.Id, 0));
            }

            // Set target actor as player actor
            _playerActor = _sceneManager.GetCurrentScene().GetActor(command.ActorId);
            _playerActorGameObject = _sceneManager.GetCurrentScene()
                .GetActorGameObject(command.ActorId);
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

            // Resetting/re-applying jumpable area enter count for new player actor
            if (IsPlayerActorInsideJumpableArea())
            {
                _jumpableAreaEnterCount--;
                PlayerActorEnteredJumpableArea();
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
                    !string.Equals(ActorConstants.ActionNames[ActorActionType.Stand],
                        _playerActorActionController.GetCurrentAction(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    _playerActorActionController.PerformAction(ActorActionType.Stand);
                }

                _lastKnownPosition = null;
                _lastKnownTilePosition = null;
            }
        }

        public void Execute(PlayerInteractionRequest _)
        {
            if (IsPlayerActorInsideJumpableArea())
            {
                StartCoroutine(JumpAsync());
            }
            else
            {
                InteractWithFacingInteractable();
            }
        }

        public void Execute(ActorSetTilePositionCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                Scene currentScene = _sceneManager.GetCurrentScene();

                var currentLayerIndex = currentScene
                    .GetActorGameObject((int)_playerManager.GetPlayerActor())
                    .GetComponent<ActorMovementController>()
                    .GetCurrentLayerIndex();

                _lastKnownPosition = currentScene.GetTilemap().GetWorldPosition(
                    new Vector2Int(command.TileXPosition,command.TileYPosition), currentLayerIndex);
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
            var playerActorId = (int)_playerManager.GetPlayerActor();

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

            if (_playerManager.IsPlayerActorControlEnabled())
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new ActorEnablePlayerControlCommand(playerActorId));
            }

            if (_playerManager.IsPlayerInputEnabled())
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new PlayerEnableInputCommand(1));
            }

            #if PAL3
            CommandDispatcher<ICommand>.Instance.Dispatch(new LongKuiSwitchModeCommand(_longKuiLastKnownMode));
            #endif
        }

        public void Execute(ResetGameStateCommand command)
        {
            _playerActorLastKnownSceneState.Clear();

            _currentMovementSfxAudioName = string.Empty;

            _lastInputTapPosition = null;
            _lastKnownPosition = null;
            _lastKnownTilePosition = null;
            _lastKnownLayerIndex = null;
            _lastKnownPlayerActorAction = string.Empty;

            #if PAL3
            _longKuiLastKnownMode = 0;
            #endif

            _playerActor = null;
            _playerActorGameObject = null;
            _playerActorController = null;
            _playerActorActionController = null;
            _playerActorMovementController = null;

            _jumpableAreaEnterCount = 0;
        }

        // TODO: Remove this
        public void Execute(ToggleBigMapRequest command)
        {
            if (_sceneManager.GetCurrentScene().GetSceneInfo().SceneType == ScnSceneType.Maze)
            {
                SwitchPlayerActorInCurrentTeam(true);
            }
        }

        #if PAL3
        public void Execute(LongKuiSwitchModeCommand command)
        {
            _longKuiLastKnownMode = command.Mode;
        }
        #endif
    }
}