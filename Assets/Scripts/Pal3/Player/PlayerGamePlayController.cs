// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
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
    using Core.DataReader.Nav;
    using Core.DataReader.Scn;
    using Core.Renderer;
    using Input;
    using MetaData;
    using Scene;
    using Scene.SceneObjects;
    using Script.Waiter;
    using State;
    using UnityEngine;
    using UnityEngine.InputSystem;

    public sealed class PlayerGamePlayController : MonoBehaviour,
        ICommandExecutor<ActorEnablePlayerControlCommand>,
        ICommandExecutor<ActorSetTilePositionCommand>,
        ICommandExecutor<PlayerEnableInputCommand>,
        ICommandExecutor<ActorPerformClimbActionCommand>,
        ICommandExecutor<PlayerActorClimbObjectCommand>,
        ICommandExecutor<PlayerInteractionRequest>,
        #if PAL3
        ICommandExecutor<LongKuiSwitchModeCommand>,
        #endif
        ICommandExecutor<SceneLeavingCurrentSceneNotification>,
        ICommandExecutor<ScenePostLoadingNotification>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private GameStateManager _gameStateManager;
        private PlayerManager _playerManager;
        private PlayerInputActions _inputActions;
        private SceneManager _sceneManager;
        private Camera _camera;

        private string _currentMovementSfxAudioName = string.Empty;
        private const string PLAYER_ACTOR_MOVEMENT_SFX_AUDIO_SOURCE_NAME = "PlayerActorMovementSfx";
        private const float PLAYER_ACTOR_MOVEMENT_SFX_WALK_VOLUME = 0.6f;
        private const float PLAYER_ACTOR_MOVEMENT_SFX_RUN_VOLUME = 1.0f;

        private Vector2? _lastInputTapPosition;
        private Vector3? _lastKnownPosition;
        private Vector2Int? _lastKnownTilePosition;
        private int? _lastKnownLayerIndex;
        private string _lastKnownPlayerActorAction = string.Empty;
        #if PAL3
        private int _longKuiLastKnownMode = 0;
        #endif
        
        private GameObject _playerActor;
        private ActorController _playerActorController;
        private ActorActionController _playerActorActionController;
        private ActorMovementController _playerActorMovementController;

        private const int LAST_KNOWN_SCENE_STATE_LIST_MAX_LENGTH = 2;
        private readonly List<(ScnSceneInfo sceneInfo,
            int actorNavIndex,
            Vector2Int actorTilePosition,
            Vector3 actorFacing)> _playerActorLastKnownSceneState = new ();

        public void Init(GameStateManager gameStateManager,
            PlayerManager playerManager,
            PlayerInputActions inputActions,
            SceneManager sceneManager,
            Camera mainCamera)
        {
            _gameStateManager = gameStateManager ?? throw new ArgumentNullException(nameof(gameStateManager));
            _playerManager = playerManager ?? throw new ArgumentNullException(nameof(playerManager));
            _inputActions = inputActions ?? throw new ArgumentNullException(nameof(inputActions));
            _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
            _camera = mainCamera != null ? mainCamera : throw new ArgumentNullException(nameof(mainCamera));

            _inputActions.Gameplay.OnTap.performed += OnTapPerformed;
            _inputActions.Gameplay.OnMove.performed += OnMovePerformed;
            _inputActions.Gameplay.OnDoubleTap.performed += OnDoubleTapPerformed;
            _inputActions.Gameplay.PortalTo.performed += PortalToPerformed;
            _inputActions.Gameplay.Interact.performed += InteractionPerformed;
            _inputActions.Gameplay.Movement.canceled += MovementCanceled;
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
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        private void Update()
        {
            if (_playerActor == null) return;
            
            var isPlayerInControl = false;

            if (_gameStateManager.GetCurrentState() == GameState.Gameplay &&
                _playerManager.IsPlayerInputEnabled() &&
                _playerManager.IsPlayerActorControlEnabled())
            {
                isPlayerInControl = true;
                ReadInputAndMovePlayerIfNeeded();
            }

            var shouldUpdatePlayerActorMovementSfx = false;
            
            Vector3 position = _playerActor.transform.position;
            var layerIndex = _playerActorMovementController.GetCurrentLayerIndex();

            if (!(position == _lastKnownPosition && layerIndex == _lastKnownLayerIndex))
            {
                _lastKnownPosition = position;

                Vector2Int tilePosition = _playerActorMovementController.GetTilePosition();
                if (!(tilePosition == _lastKnownTilePosition && layerIndex == _lastKnownLayerIndex))
                {
                    shouldUpdatePlayerActorMovementSfx = true;
                    _lastKnownTilePosition = tilePosition;
                    CommandDispatcher<ICommand>.Instance.Dispatch(
                        new PlayerActorTilePositionUpdatedNotification(tilePosition, layerIndex, !isPlayerInControl));
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
                    new StopSfxPlayingAtGameObjectRequest(_playerActor,
                        PLAYER_ACTOR_MOVEMENT_SFX_AUDIO_SOURCE_NAME,
                        disposeSource: false));
            }
            else
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new AttachSfxToGameObjectRequest(_playerActor,
                        newMovementSfxAudioFileName,
                        PLAYER_ACTOR_MOVEMENT_SFX_AUDIO_SOURCE_NAME,
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

            var movementMode = movement.magnitude < 0.7f ? 0 : 1;
            ActorActionType movementAction = movementMode == 0 ? ActorActionType.Walk : ActorActionType.Run;
            _playerActorMovementController.CancelCurrentMovement();
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
        /// This process is purely for improving the user experience.
        /// </summary>
        /// <param name="inputDirection">Camera based user input direction</param>
        /// <param name="movementMode">Player actor movement mode</param>
        /// <returns>MovementResult</returns>
        private MovementResult PlayerActorMoveTowards(Vector3 inputDirection, int movementMode)
        {
            Vector3 playerActorPosition = _playerActor.transform.position;
            MovementResult result = _playerActorMovementController.MoveTowards(
                playerActorPosition + inputDirection, movementMode);

            if (result != MovementResult.Blocked) return result;

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

            PortalToTapPosition();
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

        private void InteractionPerformed(InputAction.CallbackContext ctx)
        {
            if (!_playerManager.IsPlayerActorControlEnabled() || !_playerManager.IsPlayerInputEnabled())
            {
                return;
            }

            InteractWithNearestInteractable();
        }

        private void InteractWithNearestInteractable()
        {
            Vector3 position = _playerActorMovementController.GetWorldPosition();
            Vector2Int tilePosition = _playerActorMovementController.GetTilePosition();
            var currentLayerIndex = _playerActorMovementController.GetCurrentLayerIndex();

            var nearestInteractableDistance = float.MaxValue;
            Action interactionAction = null;

            foreach (var sceneObjectInfo in
                     _sceneManager.GetCurrentScene().GetAllActivatedSceneObjects())
            {
                SceneObject sceneObject = _sceneManager.GetCurrentScene().GetSceneObject(sceneObjectInfo.Key);

                if (sceneObject.Info.Type != ScnSceneObjectType.Climbable &&
                    sceneObject.Info.OnLayer != currentLayerIndex) continue;

                Vector3 sceneObjectPosition = sceneObjectInfo.Value.transform.position;
                var distance = Vector2.Distance(new Vector2(position.x, position.z),
                    new Vector2(sceneObjectPosition.x, sceneObjectPosition.z));

                if (sceneObject.IsInteractable(distance, tilePosition) && distance < nearestInteractableDistance)
                {
                    nearestInteractableDistance = distance;
                    interactionAction = sceneObject.Interact;
                }
            }

            // Scene object interaction have higher priority than actor interaction
            if (interactionAction != null)
            {
                interactionAction.Invoke();
                return;
            }
            
            foreach (var actorInfo in _sceneManager.GetCurrentScene().GetAllActorGameObjects())
            {
                var actorController = actorInfo.Value.GetComponent<ActorController>();
                var actorMovementController = actorInfo.Value.GetComponent<ActorMovementController>();

                if (actorMovementController.GetCurrentLayerIndex() != currentLayerIndex ||
                    actorInfo.Key == (byte)_playerManager.GetPlayerActor() ||
                    !actorController.IsActive) continue;

                Vector3 actorPosition = actorInfo.Value.transform.position;
                var distance = Vector2.Distance(new Vector2(position.x, position.z),
                    new Vector2(actorPosition.x, actorPosition.z));

                if (actorController.IsInteractable(distance) && distance < nearestInteractableDistance)
                {
                    nearestInteractableDistance = distance;
                    interactionAction = () =>
                    {
                        CommandDispatcher<ICommand>.Instance.Dispatch(
                            new PlayerInteractionTriggeredNotification());
                        CommandDispatcher<ICommand>.Instance.Dispatch(
                            new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
                        Actor actor = _sceneManager.GetCurrentScene().GetActor(actorInfo.Key);
                        CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunCommand((int)actor.Info.ScriptId));
                    };
                }
            }
            
            interactionAction?.Invoke();
        }

        private void PortalToTapPosition()
        {
            if (!_lastInputTapPosition.HasValue) return;
            
            Ray ray = _camera.ScreenPointToRay(_lastInputTapPosition.Value);

            if (!Physics.Raycast(ray, out RaycastHit hit)) return;

            var layerIndex = _sceneManager.GetCurrentScene()
                .GetMeshColliders()
                .FirstOrDefault(_ => _.Value == hit.collider)
                .Key;

            _playerActorMovementController.PortalToPosition(hit.point, layerIndex);
        }

        // Raycast caches to avoid GC
        private readonly RaycastHit[] _raycastHits = new RaycastHit[4];
        private readonly Dictionary<int, Vector3> _tapPoints = new ();
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
                var layerIndex = meshColliders.FirstOrDefault(_ => _.Value == hit.collider).Key;
                
                if (!tilemap.TryGetTile(hit.point, layerIndex, out NavTile _))
                {
                    continue;
                }
                
                Vector3 cameraPosition = _camera.transform.position;
                var distanceToCamera = Vector3.Distance(cameraPosition, hit.point);

                if (_tapPoints.ContainsKey(layerIndex))
                {
                    var existingDistance = Vector3.Distance(cameraPosition, _tapPoints[layerIndex]);
                    if (distanceToCamera < existingDistance)
                    {
                        _tapPoints[layerIndex] = hit.point;
                    }
                }
                else
                {
                    _tapPoints[layerIndex] = hit.point;
                }
            }

            if (_tapPoints.Count > 0)
            {
                _playerActorMovementController.MoveToTapPoint(_tapPoints, isDoubleTap);
            }
        }

        public void Execute(ActorPerformClimbActionCommand command)
        {
            Scene scene = _sceneManager.GetCurrentScene();
            SceneObject climbableSceneObject = scene.GetSceneObject((byte) command.ObjectId);
            GameObject climbableObject = scene.GetSceneObjectGameObject((byte) command.ObjectId);
            if (climbableObject == null)
            {
                Debug.LogError($"Scene object not found or not activated yet: {command.ObjectId}.");
                return;
            }

            Vector3 climbableObjectPosition = climbableObject.transform.position;
            Vector3 climbableObjectFacing =
                Quaternion.Euler(0f, -climbableSceneObject.Info.YRotation, 0f) * Vector3.forward;

            Vector3 lowerPosition = climbableObjectFacing.normalized * 0.5f + climbableObjectPosition;
            Vector3 lowerStandingPosition = climbableObjectFacing.normalized * 1.5f + climbableObjectPosition;
            Vector3 upperPosition = -climbableObjectFacing.normalized * 0.5f + climbableObjectPosition;
            Vector3 upperStandingPosition = -climbableObjectFacing.normalized * 1.5f + climbableObjectPosition;

            var currentPlayerLayer = _playerActorMovementController.GetCurrentLayerIndex();

            var climbUp = (command.ClimbUp == 1);
            var climbableHeight = climbableObject.GetComponentInChildren<StaticMeshRenderer>()
                                      .GetRendererBounds().max.y / 2f; // Half is enough for the animation

            Vector3 playerCurrentPosition = _playerActor.transform.position;
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

            var waiter = new WaitUntilCanceled(this);
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerWaitRequest(waiter));

            var climbAnimationOnly = command.ClimbUp != -1;
            StartCoroutine(PlayerActorMoveToClimbableObjectAndClimb(climbableObject, climbUp,
                climbAnimationOnly, climbableHeight, lowerPosition, lowerStandingPosition,
                upperPosition, upperStandingPosition, currentPlayerLayer, currentPlayerLayer, () =>
                {
                    waiter.CancelWait();
                }));
        }

        public void Execute(PlayerActorClimbObjectCommand command)
        {
            Scene scene = _sceneManager.GetCurrentScene();
            SceneObject climbableSceneObject = scene.GetSceneObject((byte) command.ObjectId);
            GameObject climbableObject = scene.GetSceneObjectGameObject((byte) command.ObjectId);
            if (climbableObject == null)
            {
                Debug.LogError($"Scene object not found or not activated yet: {command.ObjectId}.");
                return;
            }

            Vector3 climbableObjectPosition = climbableObject.transform.position;
            Vector3 climbableObjectFacing =
                Quaternion.Euler(0f, -climbableSceneObject.Info.YRotation, 0f) * Vector3.forward;

            Tilemap tileMap = scene.GetTilemap();

            int upperLayer, lowerLayer;

            if (command.CrossLayer)
            {
                upperLayer = 1;
                lowerLayer = 0;
            }
            else
            {
                var playerCurrentLayer = _playerActorMovementController.GetCurrentLayerIndex();
                upperLayer = playerCurrentLayer;
                lowerLayer = playerCurrentLayer;
            }

            Vector3 fromPosition = tileMap.GetWorldPosition(command.FromPosition, lowerLayer);
            Vector3 toPosition = tileMap.GetWorldPosition(command.ToPosition, upperLayer);

            Vector3 upperStandingPosition, lowerStandingPosition;
            if (fromPosition.y > toPosition.y)
            {
                upperStandingPosition = fromPosition;
                lowerStandingPosition = toPosition;
            }
            else
            {
                upperStandingPosition = toPosition;
                lowerStandingPosition = fromPosition;
            }

            Vector3 upperPosition = -climbableObjectFacing.normalized * 1f + climbableObjectPosition;
            Vector3 lowerPosition = climbableObjectFacing.normalized * 1f + climbableObjectPosition;
            upperPosition.y = upperStandingPosition.y;
            lowerPosition.y = lowerStandingPosition.y;

            Vector3 playerActorPosition = _playerActor.transform.position;
            var climbUp = Mathf.Abs(playerActorPosition.y - lowerPosition.y) <
                              Mathf.Abs(playerActorPosition.y - upperPosition.y);

            var climbableHeight = upperPosition.y - lowerPosition.y;

            CommandDispatcher<ICommand>.Instance.Dispatch(new PlayerEnableInputCommand(0));

            StartCoroutine(PlayerActorMoveToClimbableObjectAndClimb(climbableObject, climbUp,
                false, climbableHeight, lowerPosition, lowerStandingPosition,
                upperPosition, upperStandingPosition, lowerLayer, upperLayer, () =>
                {
                    CommandDispatcher<ICommand>.Instance.Dispatch(new PlayerEnableInputCommand(1));
                }));
        }

        private IEnumerator PlayerActorMoveToClimbableObjectAndClimb(
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
            Action callback)
        {
            yield return _playerActorMovementController
                .MoveDirectlyTo(climbUp ? lowerPosition : upperPosition, 0);

            _playerActorActionController.PerformAction(
                climbUp ? ActorActionType.Climb : ActorActionType.ClimbDown, true, 1);

            _playerActor.transform.position = new Vector3(lowerPosition.x,
                _playerActor.transform.position.y, lowerPosition.z);

            var objectRotationY = climbableObject.transform.rotation.eulerAngles.y;
            _playerActor.transform.rotation = Quaternion.Euler(0f, objectRotationY + 180f, 0f);

            if (climbUp)
            {
                var currentHeight = 0f;
                while (currentHeight < climbableHeight)
                {
                    var delta = Time.deltaTime * ActorConstants.ActorClimbSpeed;
                    currentHeight += delta;
                    _playerActor.transform.position += new Vector3(0f, delta, 0f);
                    yield return null;
                }

                if (!climbOnly)
                {
                    _playerActorMovementController.SetNavLayer(upperLayer);
                    yield return _playerActorMovementController.MoveDirectlyTo(upperStandingPosition, 0);
                    _playerActor.transform.position = upperStandingPosition;
                }
            }
            else
            {
                var currentHeight = climbableHeight;
                while (currentHeight > 0f)
                {
                    var delta = Time.deltaTime * ActorConstants.ActorClimbSpeed;
                    currentHeight -= delta;
                    _playerActor.transform.position -= new Vector3(0f, delta, 0f);
                    yield return null;
                }

                if (!climbOnly)
                {
                    _playerActorMovementController.SetNavLayer(lowerLayer);
                    yield return _playerActorMovementController.MoveDirectlyTo(lowerStandingPosition, 0);
                    _playerActor.transform.position = lowerStandingPosition;
                }
            }

            callback?.Invoke();
        }

        public void Execute(ActorEnablePlayerControlCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID) return;

            _lastKnownPosition = null;
            _lastKnownTilePosition = null;
            _lastKnownPlayerActorAction = string.Empty;
            
            _playerActor = _sceneManager.GetCurrentScene()
                .GetActorGameObject((byte) command.ActorId);
            _playerActorController = _playerActor.GetComponent<ActorController>();
            _playerActorActionController = _playerActor.GetComponent<ActorActionController>();
            _playerActorMovementController = _playerActor.GetComponent<ActorMovementController>();

            // Stop current player actor movement sfx
            _currentMovementSfxAudioName = string.Empty;
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new StopSfxPlayingAtGameObjectRequest(_playerActor,
                    PLAYER_ACTOR_MOVEMENT_SFX_AUDIO_SOURCE_NAME,
                    disposeSource: false));

            // Just to make sure actor is activated
            if (_playerManager.IsPlayerInputEnabled() &&
                !_playerActorController.IsActive)
            {
                _playerActorController.IsActive = true;
            }
        }

        public void Execute(PlayerEnableInputCommand command)
        {
            if (command.Enable == 0)
            {
                if (_playerActorMovementController != null)
                {
                    _playerActorMovementController.CancelCurrentMovement();
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

        public void Execute(PlayerInteractionRequest command)
        {
            InteractWithNearestInteractable();
        }

        public void Execute(ActorSetTilePositionCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID)
            {
                Scene currentScene = _sceneManager.GetCurrentScene();
                
                var currentLayerIndex = currentScene
                    .GetActorGameObject((byte) _playerManager.GetPlayerActor())
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
            if (_playerActor != null)
            {
                _currentMovementSfxAudioName = string.Empty;
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new StopSfxPlayingAtGameObjectRequest(_playerActor,
                        PLAYER_ACTOR_MOVEMENT_SFX_AUDIO_SOURCE_NAME,
                        disposeSource: true));   
            }

            _playerActorLastKnownSceneState.Add((
                currentScene.GetSceneInfo(),
                _playerActorMovementController.GetCurrentLayerIndex(),
                _playerActorMovementController.GetTilePosition(),
                _playerActor.transform.forward));
            
            if (_playerActorLastKnownSceneState.Count > LAST_KNOWN_SCENE_STATE_LIST_MAX_LENGTH)
            {
                _playerActorLastKnownSceneState.RemoveAt(0);
            }

            if (_gameStateManager.GetCurrentState() == GameState.Gameplay)
            {
                // Temporarily disable player input (will resume once scene is loaded)
                _inputActions.Disable();  
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
                    .GetActorGameObject((byte)playerActorId).transform.forward = actorFacing;
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

            if (_gameStateManager.GetCurrentState() == GameState.Gameplay)
            {
                // Re-enable player input with a small delay
                Invoke(nameof(EnablePlayerInputs), 0.2f);
            }
        }

        private void EnablePlayerInputs()
        {
            _inputActions.Enable();
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