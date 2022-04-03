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
    using Core.DataReader.Scn;
    using Core.Renderer;
    using Input;
    using MetaData;
    using Scene;
    using Script.Waiter;
    using State;
    using UnityEngine;
    using UnityEngine.InputSystem;

    public class PlayerGamePlayController : MonoBehaviour,
        ICommandExecutor<ActorEnablePlayerControlCommand>,
        ICommandExecutor<ActorSetTilePositionCommand>,
        ICommandExecutor<PlayerEnableInputCommand>,
        ICommandExecutor<ActorPerformClimbActionCommand>,
        ICommandExecutor<PlayerActorClimbObjectCommand>,
        ICommandExecutor<PlayerInteractionRequest>
    {
        private GameStateManager _gameStateManager;
        private PlayerManager _playerManager;
        private PlayerInputActions _inputActions;
        private SceneManager _sceneManager;
        private Camera _camera;

        private Vector2 _lastInputTapPosition;
        private Vector3 _lastKnownPosition;
        private Vector2Int _lastKnownTilePosition;
        private int _lastKnownLayerIndex;

        private GameObject _playerActor;
        private ActorActionController _playerActorActionController;
        private ActorMovementController _playerActorMovementController;

        public void Init(GameStateManager gameStateManager,
            PlayerManager playerManager,
            PlayerInputActions inputActions,
            SceneManager sceneManager,
            Camera mainCamera)
        {
            _gameStateManager = gameStateManager;
            _playerManager = playerManager;
            _inputActions = inputActions;
            _sceneManager = sceneManager;
            _camera = mainCamera;

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

            var position = _playerActor.transform.position;
            var layerIndex = _playerActorMovementController.GetCurrentLayerIndex();

            if (!(position == _lastKnownPosition && layerIndex == _lastKnownLayerIndex))
            {
                _lastKnownPosition = position;

                var tilePosition = _playerActorMovementController.GetTilePosition();
                if (!(tilePosition == _lastKnownTilePosition && layerIndex == _lastKnownLayerIndex))
                {
                    _lastKnownTilePosition = tilePosition;
                    CommandDispatcher<ICommand>.Instance.Dispatch(
                        new PlayerActorTilePositionUpdatedNotification(tilePosition, layerIndex, !isPlayerInControl));
                }
            }

            _lastKnownLayerIndex = layerIndex;
        }

        public Vector3 GetPlayerActorLastKnownPosition()
        {
            return _lastKnownPosition;
        }

        private void ReadInputAndMovePlayerIfNeeded()
        {
            var movement = _inputActions.Gameplay.Movement.ReadValue<Vector2>();
            if (movement == Vector2.zero) return;

            var movementMode = movement.magnitude < 0.7f ? 0 : 1;
            var movementAction = movementMode == 0 ? ActorActionType.Walk : ActorActionType.Run;
            _playerActorMovementController.CancelCurrentMovement();
            var cameraTransform = _camera.transform;
            var inputDirection = cameraTransform.forward * movement.y +
                                cameraTransform.right * movement.x;
            var result = PlayerActorMoveTowards(inputDirection, movementMode);
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
            var playerActorPosition = _playerActor.transform.position;
            var result = _playerActorMovementController.MoveTowards(
                playerActorPosition + inputDirection, movementMode);

            if (result != MovementResult.Blocked) return result;

            // Try change direction a little bit to see if it works
            for (var degrees = 2; degrees <= 80; degrees+= 2)
            {
                // + degrees
                {
                    var newDirection = Quaternion.Euler(0f, degrees, 0f) * inputDirection;
                    result = _playerActorMovementController.MoveTowards(
                        playerActorPosition + newDirection, movementMode);
                    if (result != MovementResult.Blocked) return result;
                }
                // - degrees
                {
                    var newDirection = Quaternion.Euler(0f, -degrees, 0f) * inputDirection;
                    result = _playerActorMovementController.MoveTowards(
                        playerActorPosition + newDirection, movementMode);
                    if (result != MovementResult.Blocked) return result;
                }
            }

            return result;
        }

        private void MovementCanceled(InputAction.CallbackContext ctx)
        {
            _playerActorActionController.PerformAction(ActorActionType.Stand);
        }

        private void OnTapPerformed(InputAction.CallbackContext ctx)
        {
            if (!_playerManager.IsPlayerActorControlEnabled() ||
                !_playerManager.IsPlayerInputEnabled())
            {
                return;
            }

            MoveToTapPosition(false);
        }

        private void OnDoubleTapPerformed(InputAction.CallbackContext ctx)
        {
            if (!_playerManager.IsPlayerActorControlEnabled() ||
                !_playerManager.IsPlayerInputEnabled())
            {
                return;
            }

            MoveToTapPosition(true);
        }

        private void PortalToPerformed(InputAction.CallbackContext ctx)
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
            var position = _playerActorMovementController.GetWorldPosition();
            var currentLayerIndex = _playerActorMovementController.GetCurrentLayerIndex();

            var nearestInteractableDistance = float.MaxValue;
            Action interactionAction = null;

            foreach (var actorInfo in _sceneManager.GetCurrentScene().GetAllActors())
            {
                var actorController = actorInfo.Value.GetComponent<ActorController>();
                var actorMovementController = actorInfo.Value.GetComponent<ActorMovementController>();

                if (actorMovementController.GetCurrentLayerIndex() != currentLayerIndex ||
                    actorInfo.Key == (byte)_playerManager.GetPlayerActor() ||
                    !actorController.IsActive) continue;

                var actorPosition = actorInfo.Value.transform.position;
                var distance = Vector2.Distance(new Vector2(position.x, position.z),
                    new Vector2(actorPosition.x, actorPosition.z));

                if (actorController.IsInteractable(distance) && distance < nearestInteractableDistance)
                {
                    nearestInteractableDistance = distance;
                    interactionAction = actorController.Interact;
                }
            }

            foreach (var sceneObjectInfo in
                     _sceneManager.GetCurrentScene().GetAllActivatedSceneObjects())
            {
                var sceneObject = _sceneManager.GetCurrentScene().GetSceneObject(sceneObjectInfo.Key);

                if (sceneObject.Info.Type != ScnSceneObjectType.Climbable &&
                    sceneObject.Info.OnLayer != currentLayerIndex) continue;

                var sceneObjectPosition = sceneObjectInfo.Value.transform.position;
                var distance = Vector2.Distance(new Vector2(position.x, position.z),
                    new Vector2(sceneObjectPosition.x, sceneObjectPosition.z));

                if (sceneObject.IsInteractable(distance) && distance < nearestInteractableDistance)
                {
                    nearestInteractableDistance = distance;
                    interactionAction = sceneObject.Interact;
                }
            }

            interactionAction?.Invoke();
        }

        private void PortalToTapPosition()
        {
            var ray = _camera.ScreenPointToRay(_lastInputTapPosition);

            if (!Physics.Raycast(ray, out var hit)) return;

            var layerIndex = _sceneManager.GetCurrentScene().GetMeshColliders().FirstOrDefault(
                c => c.Value.Contains(hit.collider)).Key;

            _playerActorMovementController.PortalToPosition(hit.point, layerIndex);
        }

        private readonly RaycastHit[] _raycastHits = new RaycastHit[4];
        private readonly Dictionary<int, Vector3> _tapPoints = new ();
        private void MoveToTapPosition(bool isDoubleTap)
        {
            var currentScene = _sceneManager.GetCurrentScene();
            var meshColliders = currentScene.GetMeshColliders();

            var ray = _camera.ScreenPointToRay(_lastInputTapPosition);

            var hitCount = Physics.RaycastNonAlloc(ray, _raycastHits, 500f);
            if (hitCount == 0) return;

            var tilemap = currentScene.GetTilemap();

            _tapPoints.Clear();
            for (var i = 0; i < hitCount; i++)
            {
                var hit = _raycastHits[i];
                var layerIndex = meshColliders.FirstOrDefault(
                    c => c.Value.Contains(hit.collider)).Key;

                var tilePosition = tilemap.GetTilePosition(hit.point, layerIndex);
                if (!tilemap.IsTilePositionInsideTileMap(tilePosition, layerIndex)) continue;
                //if (!tilemap.GetTile(tilePosition, layerIndex).IsWalkable()) continue;

                var cameraPosition = _camera.transform.position;
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
            var scene = _sceneManager.GetCurrentScene();
            var climbableSceneObject = scene.GetSceneObject((byte) command.ObjectId);
            var climbableObject = scene.GetSceneObjectGameObject((byte) command.ObjectId);
            if (climbableObject == null)
            {
                Debug.LogError($"Scene object not found or not activated yet: {command.ObjectId}.");
                return;
            }

            var climbableObjectPosition = climbableObject.transform.position;
            var climbableObjectFacing =
                Quaternion.Euler(0f, -climbableSceneObject.Info.YRotation, 0f) * Vector3.forward;

            var lowerPosition = climbableObjectFacing.normalized * 0.5f + climbableObjectPosition;
            var lowerStandingPosition = climbableObjectFacing.normalized * 1.5f + climbableObjectPosition;
            var upperPosition = -climbableObjectFacing.normalized * 0.5f + climbableObjectPosition;
            var upperStandingPosition = -climbableObjectFacing.normalized * 1.5f + climbableObjectPosition;

            var currentPlayerLayer = _playerActorMovementController.GetCurrentLayerIndex();

            var climbUp = (command.ClimbUp == 1);
            var climbableHeight = climbableObject.GetComponentInChildren<StaticMeshRenderer>()
                                      .GetRendererBounds().max.y / 2f; // Half is enough for the animation

            var playerCurrentPosition = _playerActor.transform.position;
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
            var scene = _sceneManager.GetCurrentScene();
            var climbableSceneObject = scene.GetSceneObject((byte) command.ObjectId);
            var climbableObject = scene.GetSceneObjectGameObject((byte) command.ObjectId);
            if (climbableObject == null)
            {
                Debug.LogError($"Scene object not found or not activated yet: {command.ObjectId}.");
                return;
            }

            var climbableObjectPosition = climbableObject.transform.position;
            var climbableObjectFacing =
                Quaternion.Euler(0f, -climbableSceneObject.Info.YRotation, 0f) * Vector3.forward;

            var tileMap = scene.GetTilemap();

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

            var fromPosition = tileMap.GetWorldPosition(command.FromPosition, lowerLayer);
            var toPosition = tileMap.GetWorldPosition(command.ToPosition, upperLayer);

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

            var upperPosition = -climbableObjectFacing.normalized * 1f + climbableObjectPosition;
            var lowerPosition = climbableObjectFacing.normalized * 1f + climbableObjectPosition;
            upperPosition.y = upperStandingPosition.y;
            lowerPosition.y = lowerStandingPosition.y;

            var playerActorPosition = _playerActor.transform.position;
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

            _lastKnownPosition = Vector3.zero;
            _lastKnownTilePosition = Vector2Int.zero;

            _playerActor = _sceneManager.GetCurrentScene()
                .GetActorGameObject((byte) command.ActorId);
            _playerActorActionController = _playerActor.GetComponent<ActorActionController>();
            _playerActorMovementController = _playerActor.GetComponent<ActorMovementController>();
        }

        public void Execute(PlayerEnableInputCommand command)
        {
            if (command.Enable == 0)
            {
                if (_playerActorMovementController != null)
                {
                    _playerActorMovementController.CancelCurrentMovement();
                }

                if (_playerActorActionController != null)
                {
                    _playerActorActionController.PerformAction(ActorActionType.Stand);
                }

                _lastKnownPosition = Vector3.zero;
                _lastKnownTilePosition = Vector2Int.zero;
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
                var currentScene = _sceneManager.GetCurrentScene();
                var currentLayerIndex = currentScene
                    .GetActorGameObject((byte) _playerManager.GetPlayerActor())
                    .GetComponent<ActorMovementController>()
                    .GetCurrentLayerIndex();
                _lastKnownPosition = currentScene.GetTilemap().GetWorldPosition(
                    new Vector2Int(command.TileXPosition,command.TileZPosition), currentLayerIndex);
            }
        }
    }
}