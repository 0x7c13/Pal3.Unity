// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Camera
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Core.Primitives;
    using Core.Utilities;
    using Engine.Animation;
    using Engine.Extensions;
    using Engine.Utilities;
    using GamePlay;
    using Input;
    using Scene;
    using Script.Waiter;
    using State;
    using UnityEngine;
    using UnityEngine.InputSystem.OnScreen;
    using UnityEngine.UI;
    using Color = UnityEngine.Color;

    public sealed class CameraManager : IDisposable,
        ICommandExecutor<CameraSetTransformCommand>,
        ICommandExecutor<CameraSetDefaultTransformCommand>,
        ICommandExecutor<CameraShakeEffectCommand>,
        ICommandExecutor<CameraOrbitCommand>,
        ICommandExecutor<CameraRotateCommand>,
        ICommandExecutor<CameraOrbitHorizontalCommand>,
        ICommandExecutor<CameraOrbitVerticalCommand>,
        #if PAL3A
        ICommandExecutor<CameraMoveToLookAtPointCommand>,
        ICommandExecutor<CameraMoveToDefaultLookAtPointCommand>,
        #endif
        ICommandExecutor<CameraFadeInCommand>,
        ICommandExecutor<CameraFadeInWhiteCommand>,
        ICommandExecutor<CameraFadeOutCommand>,
        ICommandExecutor<CameraFadeOutWhiteCommand>,
        ICommandExecutor<CameraPushCommand>,
        ICommandExecutor<CameraMoveCommand>,
        ICommandExecutor<CameraFollowPlayerCommand>,
        ICommandExecutor<CameraSetYawCommand>,
        ICommandExecutor<CameraFocusOnActorCommand>,
        ICommandExecutor<CameraFocusOnSceneObjectCommand>,
        ICommandExecutor<ScenePreLoadingNotification>,
        ICommandExecutor<SceneLeavingCurrentSceneNotification>,
        ICommandExecutor<GameStateChangedNotification>,
        ICommandExecutor<CameraSetInitialStateOnNextSceneLoadCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private const float FADE_ANIMATION_DURATION = 3f;
        private const float SCENE_IN_DOOR_ROOM_FLOOR_HEIGHT = 1.6f;
        private const float CAMERA_DEFAULT_DISTANCE = 46f;
        private const float CAMERA_IN_DOOR_DISTANCE = 34f;
        private const float CAMERA_ROTATION_SPEED_KEY_PRESS = 120f;
        private const float CAMERA_ROTATION_SPEED_SCROLL = 15f;
        private const float CAMERA_ROTATION_SPEED_DRAG = 10f;
        private const float CAMERA_SMOOTH_FOLLOW_TIME = 0.2f;
        private const float CAMERA_SMOOTH_FOLLOW_MAX_DISTANCE = 7f;

        private readonly Camera _camera;
        private readonly Image _curtainImage;

        private GameObject _lookAtGameObject;
        private Vector3 _lastLookAtPoint = Vector3.zero;
        private Vector3 _cameraMoveVelocity = Vector3.zero;
        private Vector3 _actualPosition = Vector3.zero;
        private Vector3 _actualLookAtPoint = Vector3.zero;
        private Vector3 _cameraOffset = Vector3.zero;
        private float _lookAtPointYOffset;

        private bool _freeToRotate;
        private bool _cameraFollowPlayer = true;
        private bool _shouldResetVelocity = false;
        private int _currentAppliedDefaultTransformOption = 0;

        private readonly PlayerInputActions _inputActions;
        private readonly PlayerGamePlayManager _gamePlayManager;
        private readonly SceneManager _sceneManager;
        private readonly GameStateManager _gameStateManager;

        private readonly RectTransform _joyStickRectTransform;
        private readonly float _joyStickMovementRange;
        private readonly bool _isTouchEnabled;

        private bool _cameraAnimationInProgress;
        private CancellationTokenSource _asyncCameraAnimationCts = new ();
        private CancellationTokenSource _cameraFadeAnimationCts = new ();

        private Quaternion? _initRotationOnSceneLoad = null;
        private int? _initTransformOptionOnSceneLoad = null;

        private const int CAMERA_LAST_KNOWN_STATE_LIST_MAX_LENGTH = 1;
        private readonly List<(ScnSceneInfo sceneInfo,
            Quaternion cameraRotation,
            int transformOption)> _cameraLastKnownState = new ();

        public CameraManager(PlayerInputActions inputActions,
            PlayerGamePlayManager gamePlayManager,
            SceneManager sceneManager,
            GameStateManager gameStateManager,
            Camera mainCamera,
            Canvas touchControlUI,
            Image curtainImage)
        {
            _inputActions = Requires.IsNotNull(inputActions, nameof(inputActions));
            _gamePlayManager = Requires.IsNotNull(gamePlayManager, nameof(gamePlayManager));
            _sceneManager = Requires.IsNotNull(sceneManager, nameof(sceneManager));
            _gameStateManager = Requires.IsNotNull(gameStateManager, nameof(gameStateManager));

            _camera = mainCamera;
            _camera!.fieldOfView = HorizontalToVerticalFov(24.05f, 4f/3f);

            _curtainImage = curtainImage;

            _isTouchEnabled = UnityEngineUtility.IsHandheldDevice();

            var onScreenStick = touchControlUI.GetComponentInChildren<OnScreenStick>();
            _joyStickMovementRange = onScreenStick.movementRange;
            var joyStickImage = onScreenStick.gameObject.GetComponent<Image>();
            _joyStickRectTransform = joyStickImage.rectTransform;

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void LateUpdate(float deltaTime)
        {
            if (!_cameraFollowPlayer)
            {
                _shouldResetVelocity = true;
                return;
            }

            if (_cameraAnimationInProgress) return;

            if (_lookAtGameObject != null)
            {
                _lastLookAtPoint = _lookAtGameObject.transform.position;
            }
            else if (_gamePlayManager.TryGetPlayerActorLastKnownPosition(out Vector3 playerActorPosition))
            {
                _lastLookAtPoint = playerActorPosition;
            }

            var yOffset = new Vector3(0f, _lookAtPointYOffset, 0f);
            Vector3 targetPosition = _lastLookAtPoint + _cameraOffset;
            Vector3 currentPosition = _camera.transform.position;

            Vector3 previousLookAtPoint = _lastLookAtPoint + (currentPosition - targetPosition);

            if (Vector3.Distance(targetPosition, currentPosition) > CAMERA_SMOOTH_FOLLOW_MAX_DISTANCE ||
                _shouldResetVelocity)
            {
                _actualPosition = targetPosition;
                _actualLookAtPoint = _lastLookAtPoint;
                if (_shouldResetVelocity)
                {
                    _cameraMoveVelocity = Vector3.zero;
                    _shouldResetVelocity = false;
                }
            }
            else
            {
                _actualPosition = Vector3.SmoothDamp(currentPosition,
                    targetPosition, ref _cameraMoveVelocity, CAMERA_SMOOTH_FOLLOW_TIME);
                _actualLookAtPoint = previousLookAtPoint + (_actualPosition - currentPosition);
            }

            _camera.transform.position = _actualPosition;

            if (_lookAtGameObject != null) return;

            if (!_freeToRotate)
            {
                _camera.transform.LookAt(_actualLookAtPoint + yOffset);
                return;
            }

            RotateCameraBasedOnUserInput(deltaTime);
        }

        private void RotateCameraBasedOnUserInput(float deltaTime)
        {
            if (_inputActions.Gameplay.RotateCameraClockwise.inProgress)
            {
                RotateToOrbitPoint(deltaTime * CAMERA_ROTATION_SPEED_KEY_PRESS);
            }
            else if (_inputActions.Gameplay.RotateCameraCounterClockwise.inProgress)
            {
                RotateToOrbitPoint(-deltaTime * CAMERA_ROTATION_SPEED_KEY_PRESS);
            }

            var mouseScroll = _inputActions.Gameplay.OnScroll.ReadValue<float>();
            if (mouseScroll != 0)
            {
                RotateToOrbitPoint(mouseScroll * deltaTime * CAMERA_ROTATION_SPEED_SCROLL);
            }

            if (!_isTouchEnabled) return;

            var touch0Delta = _inputActions.Gameplay.Touch0Delta.ReadValue<float>();
            if (touch0Delta != 0)
            {
                var startPosition = _inputActions.Gameplay.Touch0Start.ReadValue<Vector2>();

                if (IsTouchPositionOutsideVirtualJoyStickRange(startPosition))
                {
                    RotateToOrbitPoint(touch0Delta * deltaTime * CAMERA_ROTATION_SPEED_DRAG);
                }
            }

            var touch1Delta = _inputActions.Gameplay.Touch1Delta.ReadValue<float>();
            if (touch1Delta != 0)
            {
                var startPosition = _inputActions.Gameplay.Touch1Start.ReadValue<Vector2>();

                if (IsTouchPositionOutsideVirtualJoyStickRange(startPosition))
                {
                    RotateToOrbitPoint(touch1Delta * deltaTime * CAMERA_ROTATION_SPEED_DRAG);
                }
            }
        }

        private bool IsTouchPositionOutsideVirtualJoyStickRange(Vector2 touchPosition)
        {
            if (touchPosition == Vector2.zero) return false;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _joyStickRectTransform, touchPosition, null, out Vector2 localPoint);

            Rect joyStickRect = _joyStickRectTransform.rect;

            return localPoint.x < -_joyStickMovementRange ||
                   localPoint.x > joyStickRect.width + _joyStickMovementRange ||
                   localPoint.y < -_joyStickMovementRange ||
                   localPoint.y > joyStickRect.height + _joyStickMovementRange;
        }

        private void RotateToOrbitPoint(float yaw)
        {
            var yOffset = new Vector3(0f, _lookAtPointYOffset, 0f);
            _cameraOffset = Quaternion.AngleAxis(yaw, Vector3.up) * _cameraOffset;
            _camera.transform.position = _actualLookAtPoint + _cameraOffset;
            _camera.transform.LookAt(_actualLookAtPoint + yOffset);
        }

        private static float HorizontalToVerticalFov(float horizontalFov, float aspect)
        {
            return Mathf.Rad2Deg * 2 * Mathf.Atan(Mathf.Tan((horizontalFov * Mathf.Deg2Rad) / 2f) / aspect);
        }

        private IEnumerator ShakeAsync(float duration, float amplitude, Action onFinished = null)
        {
            _cameraAnimationInProgress = true;

            yield return _camera.transform.ShakeAsync(duration,
                amplitude,
                false,
                true,
                false);

            _cameraAnimationInProgress = false;
            onFinished?.Invoke();
        }

        private IEnumerator MoveAsync(Vector3 position,
            float duration,
            AnimationCurveType curveType,
            Action onFinished = null,
            CancellationToken cancellationToken = default)
        {
            _cameraAnimationInProgress = true;
            Transform cameraTransform = _camera.transform;
            Vector3 oldPosition = cameraTransform.position;

            yield return cameraTransform.MoveAsync(position,
                duration,
                curveType,
                cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
            {
                _lastLookAtPoint += position - oldPosition;
                _cameraOffset = _camera.transform.position - _lastLookAtPoint;
            }
            _cameraAnimationInProgress = false;
            onFinished?.Invoke();
        }

        private IEnumerator OrbitAsync(Quaternion toRotation,
            float duration,
            AnimationCurveType curveType,
            float distanceDelta,
            Action onFinished = null,
            CancellationToken cancellationToken = default)
        {
            _cameraAnimationInProgress = true;
            Vector3 lookAtPoint = _lastLookAtPoint;
            yield return _camera.transform.OrbitAroundCenterPointAsync(toRotation,
                lookAtPoint,
                duration,
                curveType,
                distanceDelta,
                cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
            {
                _cameraOffset = _camera.transform.position - lookAtPoint;
            }
            _cameraAnimationInProgress = false;
            onFinished?.Invoke();
        }

        private IEnumerator RotateAsync(Quaternion toRotation,
            float duration,
            AnimationCurveType curveType,
            Action onFinished = null,
            CancellationToken cancellationToken = default)
        {
            _cameraAnimationInProgress = true;
            yield return _camera.transform.RotateAsync(toRotation,
                duration,
                curveType,
                cancellationToken);
            _cameraAnimationInProgress = false;
            onFinished?.Invoke();
        }

        public IEnumerator PushAsync(float distance,
            float duration,
            AnimationCurveType curveType,
            Action onFinished = null,
            CancellationToken cancellationToken = default)
        {
            _cameraAnimationInProgress = true;
            Vector3 oldPosition = _camera.transform.position;
            var oldDistance = Vector3.Distance(oldPosition, _lastLookAtPoint);
            Vector3 cameraFacingDirection = (oldPosition - _lastLookAtPoint).normalized;
            Vector3 newPosition = oldPosition + cameraFacingDirection * (distance - oldDistance);
            Transform cameraTransform = _camera.transform;

            yield return cameraTransform.MoveAsync(newPosition,
                duration,
                curveType,
                cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
            {
                _cameraOffset = cameraTransform.position - _lastLookAtPoint;
            }
            _cameraAnimationInProgress = false;
            onFinished?.Invoke();
        }

        private IEnumerator FadeAsync(bool fadeIn,
            float duration,
            Color color,
            Action onFinished = null,
            CancellationToken cancellationToken = default)
        {
            _curtainImage.color = color;

            float from = 1f, to = 0f;
            if (!fadeIn) { from = 0f; to = 1f; }

            yield return CoreAnimation.EnumerateValueAsync(from, to, duration, AnimationCurveType.Linear,
                alpha =>
            {
                _curtainImage.color = new Color(color.r, color.g, color.b, alpha);
            }, cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
            {
                _curtainImage.color = new Color(color.r, color.g, color.b, to);
            }

            onFinished?.Invoke();
        }

        public int GetCurrentAppliedDefaultTransformOption()
        {
            return _currentAppliedDefaultTransformOption;
        }

        public Camera GetMainCamera()
        {
            return _camera;
        }

        private void ApplySceneSettings(ScnSceneInfo sceneInfo,
            int? initTransformOption = null, Quaternion? initRotation = null)
        {
            switch (sceneInfo.SceneType)
            {
                case SceneType.OutDoor:
                    _lookAtPointYOffset = 0;
                    _camera.nearClipPlane = 2f;
                    _camera.farClipPlane = 800f;
                    ApplyDefaultSettings(initTransformOption ?? 0, initRotation);
                    break;
                case SceneType.InDoor:
                    _lookAtPointYOffset = SCENE_IN_DOOR_ROOM_FLOOR_HEIGHT;
                    _camera.nearClipPlane = 1f;
                    _camera.farClipPlane = 500f;
                    ApplyDefaultSettings(initTransformOption ?? 1, initRotation);
                    break;
                case SceneType.Maze:
                    _lookAtPointYOffset = 0;
                    _camera.nearClipPlane = 2f;
                    _camera.farClipPlane = 800f;
                    ApplyDefaultSettings(initTransformOption ?? 0, initRotation);
                    break;
                default:
                    _lookAtPointYOffset = 0;
                    _camera.nearClipPlane = 1f;
                    _camera.farClipPlane = 500f;
                    ApplyDefaultSettings(initTransformOption ?? 0, initRotation);
                    break;
            }

            _shouldResetVelocity = true;
        }

        private void ApplyDefaultSettings(int option, Quaternion? initRotation = null)
        {
            _currentAppliedDefaultTransformOption = option;

            float cameraDistance;
            Quaternion cameraRotation;
            float cameraFov;

            switch (option)
            {
                case 0:
                    _freeToRotate = true;
                    cameraFov = HorizontalToVerticalFov(26.0f, 4f/3f);
                    cameraDistance = CAMERA_DEFAULT_DISTANCE;
                    cameraRotation = initRotation ?? UnityPrimitivesConvertor.ToUnityQuaternion(-30.37f, -52.65f, 0f);
                    break;
                case 1:
                    _freeToRotate = false;
                    cameraFov = HorizontalToVerticalFov(24.05f, 4f/3f);
                    cameraDistance = CAMERA_IN_DOOR_DISTANCE;
                    cameraRotation = initRotation ?? UnityPrimitivesConvertor.ToUnityQuaternion(-19.48f, 33.24f, 0f);
                    break;
                case 2:
                    _freeToRotate = false;
                    cameraFov = HorizontalToVerticalFov(24.05f, 4f/3f);
                    cameraDistance = CAMERA_IN_DOOR_DISTANCE;
                    cameraRotation = initRotation ?? UnityPrimitivesConvertor.ToUnityQuaternion(-19.48f, -33.24f, 0f);
                    break;
                case 3:
                    _freeToRotate = false;
                    cameraFov = HorizontalToVerticalFov(24.05f, 4f/3f);
                    cameraDistance = CAMERA_IN_DOOR_DISTANCE;
                    cameraRotation = initRotation ?? UnityPrimitivesConvertor.ToUnityQuaternion(-19.48f, 0f, 0f);
                    break;
                default:
                    return;
            }

            _camera.fieldOfView = cameraFov;

            var yOffset = new Vector3(0f, _lookAtPointYOffset, 0f);

            Vector3 cameraFacingDirection = (cameraRotation * Vector3.forward).normalized;
            Vector3 cameraPosition = _lastLookAtPoint + cameraFacingDirection * -cameraDistance + yOffset;

            _camera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
            _cameraOffset = cameraPosition - _lastLookAtPoint;
        }

        public void Execute(CameraSetDefaultTransformCommand command)
        {
            if (!_asyncCameraAnimationCts.IsCancellationRequested) _asyncCameraAnimationCts.Cancel();

            if (_gamePlayManager.TryGetPlayerActorLastKnownPosition(out Vector3 playerActorPosition))
            {
                _lastLookAtPoint = playerActorPosition;
            }

            ApplyDefaultSettings(command.Option);
            _cameraFollowPlayer = true;
        }

        public void Execute(CameraSetTransformCommand command)
        {
            if (!_asyncCameraAnimationCts.IsCancellationRequested) _asyncCameraAnimationCts.Cancel();

            _lookAtGameObject = null;

            Vector3 cameraPosition = new GameBoxVector3(
                command.GameBoxXPosition,
                command.GameBoxYPosition,
                command.GameBoxZPosition).ToUnityPosition();
            Quaternion cameraRotation = UnityPrimitivesConvertor.ToUnityQuaternion(command.Pitch, command.Yaw, 0f);

            Transform cameraTransform = _camera.transform;
            cameraTransform.SetPositionAndRotation(cameraPosition, cameraRotation);

            _lastLookAtPoint = cameraPosition +
                               cameraTransform.forward * command.GameBoxDistance.ToUnityDistance();
            _cameraOffset = cameraPosition - _lastLookAtPoint;

            if (_gameStateManager.GetCurrentState() != GameState.Gameplay)
            {
                _cameraFollowPlayer = false;
            }
        }

        public void Execute(CameraFollowPlayerCommand command)
        {
            if (!_asyncCameraAnimationCts.IsCancellationRequested) _asyncCameraAnimationCts.Cancel();

            _lookAtGameObject = null;
            _cameraFollowPlayer = command.Follow == 1;
        }

        public void Execute(CameraShakeEffectCommand command)
        {
            if (!_asyncCameraAnimationCts.IsCancellationRequested) _asyncCameraAnimationCts.Cancel();

            var waiter = new WaitUntilCanceled();
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(waiter));
            Pal3.Instance.StartCoroutine(ShakeAsync(command.Duration,
                command.Amplitude.ToUnityDistance(),
                () => waiter.CancelWait()));
        }

        public void Execute(CameraOrbitCommand command)
        {
            if (!_asyncCameraAnimationCts.IsCancellationRequested) _asyncCameraAnimationCts.Cancel();

            var waiter = new WaitUntilCanceled();
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(waiter));
            Quaternion rotation = UnityPrimitivesConvertor.ToUnityQuaternion(command.Pitch, command.Yaw, 0f);
            Pal3.Instance.StartCoroutine(OrbitAsync(rotation,
                command.Duration,
                (AnimationCurveType)command.CurveType,
                0f,
                () => waiter.CancelWait()));
        }

        public void Execute(CameraRotateCommand command)
        {
            if (!_asyncCameraAnimationCts.IsCancellationRequested) _asyncCameraAnimationCts.Cancel();

            #if PAL3
            if (true)
            #elif PAL3A
            if (command.Synchronous == 1)
            #endif
            {
                var waiter = new WaitUntilCanceled();
                CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(waiter));
                Quaternion rotation = UnityPrimitivesConvertor.ToUnityQuaternion(command.Pitch, command.Yaw, 0f);
                Pal3.Instance.StartCoroutine(RotateAsync(rotation,
                    command.Duration,
                    (AnimationCurveType)command.CurveType,
                    () => waiter.CancelWait()));
            }
            #if PAL3A
            else
            {
                _asyncCameraAnimationCts = new CancellationTokenSource();
                Quaternion rotation = UnityPrimitivesConvertor.ToUnityQuaternion(command.Pitch, command.Yaw, 0f);
                Pal3.Instance.StartCoroutine(RotateAsync(rotation,
                    command.Duration,
                    (AnimationCurveType)command.CurveType,
                    onFinished: null,
                    _asyncCameraAnimationCts.Token));
            }
            #endif
        }

        public void Execute(CameraFadeInCommand command)
        {
            if (!_cameraFadeAnimationCts.IsCancellationRequested) _cameraFadeAnimationCts.Cancel();
            _cameraFadeAnimationCts = new CancellationTokenSource();
            var waiter = new WaitUntilCanceled();
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(waiter));
            Pal3.Instance.StartCoroutine(FadeAsync(true,
                FADE_ANIMATION_DURATION,
                Color.black,
                () => waiter.CancelWait(),
                _cameraFadeAnimationCts.Token));
        }

        public void Execute(CameraFadeInWhiteCommand command)
        {
            if (!_cameraFadeAnimationCts.IsCancellationRequested) _cameraFadeAnimationCts.Cancel();
            _cameraFadeAnimationCts = new CancellationTokenSource();
            var waiter = new WaitUntilCanceled();
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(waiter));
            Pal3.Instance.StartCoroutine(FadeAsync(true,
                FADE_ANIMATION_DURATION,
                Color.white,
                () => waiter.CancelWait(),
                _cameraFadeAnimationCts.Token));
        }

        public void Execute(CameraFadeOutCommand command)
        {
            if (!_cameraFadeAnimationCts.IsCancellationRequested) _cameraFadeAnimationCts.Cancel();
            _cameraFadeAnimationCts = new CancellationTokenSource();
            var waiter = new WaitUntilCanceled();
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(waiter));
            Pal3.Instance.StartCoroutine(FadeAsync(false,
                FADE_ANIMATION_DURATION,
                Color.black,
                () => waiter.CancelWait(),
                _cameraFadeAnimationCts.Token));
        }

        public void Execute(CameraFadeOutWhiteCommand command)
        {
            if (!_cameraFadeAnimationCts.IsCancellationRequested) _cameraFadeAnimationCts.Cancel();
            _cameraFadeAnimationCts = new CancellationTokenSource();
            var waiter = new WaitUntilCanceled();
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(waiter));
            Pal3.Instance.StartCoroutine(FadeAsync(false,
                FADE_ANIMATION_DURATION,
                Color.white,
                () => waiter.CancelWait(),
                _cameraFadeAnimationCts.Token));
        }

        public void Execute(CameraPushCommand command)
        {
            if (!_asyncCameraAnimationCts.IsCancellationRequested) _asyncCameraAnimationCts.Cancel();

            #if PAL3
            if (true)
            #elif PAL3A
            if (command.Synchronous == 1)
            #endif
            {
                var waiter = new WaitUntilCanceled();
                CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(waiter));
                var distance = command.GameBoxDistance.ToUnityDistance();
                Pal3.Instance.StartCoroutine(PushAsync(distance,
                    command.Duration,
                    (AnimationCurveType)command.CurveType,
                    () => waiter.CancelWait()));
            }
            #if PAL3A
            else
            {
                _asyncCameraAnimationCts = new CancellationTokenSource();
                var distance = command.GameBoxDistance.ToUnityDistance();
                Pal3.Instance.StartCoroutine(PushAsync(distance,
                    command.Duration,
                    (AnimationCurveType)command.CurveType,
                    onFinished: null,
                    _asyncCameraAnimationCts.Token));
            }
            #endif
        }

        public void Execute(CameraMoveCommand command)
        {
            if (!_asyncCameraAnimationCts.IsCancellationRequested) _asyncCameraAnimationCts.Cancel();

            #if PAL3
            if (true)
            #elif PAL3A
            if (command.Synchronous == 1)
            #endif
            {
                var waiter = new WaitUntilCanceled();
                CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(waiter));
                Vector3 position = new GameBoxVector3(
                    command.GameBoxXPosition,
                    command.GameBoxYPosition,
                    command.GameBoxZPosition).ToUnityPosition();
                Pal3.Instance.StartCoroutine(MoveAsync(position,
                    command.Duration,
                    (AnimationCurveType)command.CurveType,
                    () => waiter.CancelWait()));
            }
            #if PAL3A
            else
            {
                _asyncCameraAnimationCts = new CancellationTokenSource();
                Vector3 position = new GameBoxVector3(
                    command.GameBoxXPosition,
                    command.GameBoxYPosition,
                    command.GameBoxZPosition).ToUnityPosition();
                Pal3.Instance.StartCoroutine(MoveAsync(position,
                    command.Duration,
                    (AnimationCurveType)command.CurveType,
                    onFinished: null,
                    _asyncCameraAnimationCts.Token));
            }
            #endif
        }

        public void Execute(CameraSetYawCommand command)
        {
            if (!_asyncCameraAnimationCts.IsCancellationRequested) _asyncCameraAnimationCts.Cancel();
            RotateToOrbitPoint(command.Yaw);
        }

        public void Execute(SceneLeavingCurrentSceneNotification command)
        {
            if (_sceneManager.GetCurrentScene() is not { } currentScene) return;

            // Remember the current scene's camera position and rotation
            if (_cameraFollowPlayer && currentScene.GetSceneInfo().SceneType != SceneType.InDoor)
            {
                var cameraTransform = _camera.transform;
                _cameraLastKnownState.Add((
                    currentScene.GetSceneInfo(),
                    cameraTransform.rotation,
                    _currentAppliedDefaultTransformOption));

                if (_cameraLastKnownState.Count > CAMERA_LAST_KNOWN_STATE_LIST_MAX_LENGTH)
                {
                    _cameraLastKnownState.RemoveAt(0);
                }
            }
        }

        public void Execute(CameraSetInitialStateOnNextSceneLoadCommand command)
        {
            _initRotationOnSceneLoad = Quaternion.Euler(command.InitRotationInEulerAngles);
            _initTransformOptionOnSceneLoad = command.InitTransformOption;
        }

        public void Execute(ScenePreLoadingNotification notification)
        {
            Quaternion? initRotation = _initRotationOnSceneLoad;
            int? initTransformOption = _initTransformOptionOnSceneLoad;

            // Use the last known camera rotation based on last known state found in record.
            if (_cameraFollowPlayer && notification.NewSceneInfo.SceneType != SceneType.InDoor)
            {
                if (_cameraLastKnownState.Count > 0 && _cameraLastKnownState.Any(_ =>
                        _.sceneInfo.ModelEquals(notification.NewSceneInfo)))
                {
                    (ScnSceneInfo _, Quaternion cameraRotation, int transformOption) =
                        _cameraLastKnownState.Last(_ => _.sceneInfo.ModelEquals(notification.NewSceneInfo));

                    initRotation = cameraRotation;
                    initTransformOption = transformOption;
                }
            }

            ApplySceneSettings(notification.NewSceneInfo, initTransformOption, initRotation);

            // Reset
            _initTransformOptionOnSceneLoad = null;
            _initRotationOnSceneLoad = null;
        }

        public void Execute(CameraFocusOnActorCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID) return;
            if (!_asyncCameraAnimationCts.IsCancellationRequested) _asyncCameraAnimationCts.Cancel();
            _lookAtGameObject = _sceneManager.GetCurrentScene().GetActorGameObject(command.ActorId);
        }

        public void Execute(CameraFocusOnSceneObjectCommand command)
        {
            if (!_asyncCameraAnimationCts.IsCancellationRequested) _asyncCameraAnimationCts.Cancel();

            _lookAtGameObject = _sceneManager.GetCurrentScene()
                .GetSceneObject(command.SceneObjectId).GetGameObject();
        }

        public void Execute(GameStateChangedNotification command)
        {
            if (command.NewState == GameState.Gameplay)
            {
                _cameraFollowPlayer = true;
            }
        }

        public void Execute(ResetGameStateCommand command)
        {
            if (!_asyncCameraAnimationCts.IsCancellationRequested) _asyncCameraAnimationCts.Cancel();

            _curtainImage.color = Color.black;
            _currentAppliedDefaultTransformOption = 0;
            _initTransformOptionOnSceneLoad = null;
            _initRotationOnSceneLoad = null;
            _cameraLastKnownState.Clear();
            _cameraFollowPlayer = true;
        }

        public void Execute(CameraOrbitHorizontalCommand command)
        {
            if (!_asyncCameraAnimationCts.IsCancellationRequested) _asyncCameraAnimationCts.Cancel();

            var oldDistance = _cameraOffset.magnitude;
            var newDistance = command.GameBoxDistance.ToUnityDistance();
            var distanceDelta = newDistance - oldDistance;

            if (command.Synchronous == 1)
            {
                var waiter = new WaitUntilCanceled();
                CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(waiter));
                Quaternion rotation = UnityPrimitivesConvertor.ToUnityQuaternion(command.Pitch, command.Yaw, 0f);
                Pal3.Instance.StartCoroutine(OrbitAsync(rotation,
                    command.Duration,
                    (AnimationCurveType)command.CurveType,
                    distanceDelta,
                    () => waiter.CancelWait()));
            }
            else
            {
                _asyncCameraAnimationCts = new CancellationTokenSource();
                Quaternion rotation = UnityPrimitivesConvertor.ToUnityQuaternion(command.Pitch, command.Yaw, 0f);
                Pal3.Instance.StartCoroutine(OrbitAsync(rotation,
                    command.Duration,
                    (AnimationCurveType)command.CurveType,
                    distanceDelta,
                    onFinished: null,
                    _asyncCameraAnimationCts.Token));
            }
        }

        public void Execute(CameraOrbitVerticalCommand command)
        {
            if (!_asyncCameraAnimationCts.IsCancellationRequested) _asyncCameraAnimationCts.Cancel();

            var oldDistance = _cameraOffset.magnitude;
            var newDistance = command.GameBoxDistance.ToUnityDistance();
            var distanceDelta = newDistance - oldDistance;

            if (command.Synchronous == 1)
            {
                var waiter = new WaitUntilCanceled();
                CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(waiter));
                Quaternion rotation = UnityPrimitivesConvertor.ToUnityQuaternion(command.Pitch, command.Yaw, 0f);
                Pal3.Instance.StartCoroutine(OrbitAsync(rotation,
                    command.Duration,
                    (AnimationCurveType)command.CurveType,
                    distanceDelta,
                    () => waiter.CancelWait()));
            }
            else
            {
                _asyncCameraAnimationCts = new CancellationTokenSource();
                Quaternion rotation = UnityPrimitivesConvertor.ToUnityQuaternion(command.Pitch, command.Yaw, 0f);
                Pal3.Instance.StartCoroutine(OrbitAsync(rotation,
                    command.Duration,
                    (AnimationCurveType)command.CurveType,
                    distanceDelta,
                    onFinished: null,
                    _asyncCameraAnimationCts.Token));
            }
        }

        #if PAL3A
        public void Execute(CameraMoveToLookAtPointCommand command)
        {
            if (!_asyncCameraAnimationCts.IsCancellationRequested) _asyncCameraAnimationCts.Cancel();

            _cameraFollowPlayer = false;
            const float duration = 1f;

            if (command.Synchronous == 1)
            {
                var waiter = new WaitUntilCanceled();
                CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(waiter));
                Vector3 position = _cameraOffset + new GameBoxVector3(
                    command.GameBoxXPosition,
                    command.GameBoxYPosition,
                    command.GameBoxZPosition).ToUnityPosition();
                Pal3.Instance.StartCoroutine(MoveAsync(position,
                    duration,
                    AnimationCurveType.Sine,
                    () => waiter.CancelWait()));
            }
            else
            {
                _asyncCameraAnimationCts = new CancellationTokenSource();
                Vector3 position = _cameraOffset + new GameBoxVector3(
                    command.GameBoxXPosition,
                    command.GameBoxYPosition,
                    command.GameBoxZPosition).ToUnityPosition();
                Pal3.Instance.StartCoroutine(MoveAsync(position,
                    duration,
                    AnimationCurveType.Sine,
                    onFinished: null,
                    _asyncCameraAnimationCts.Token));
            }
        }

        public void Execute(CameraMoveToDefaultLookAtPointCommand command)
        {
            if (!_asyncCameraAnimationCts.IsCancellationRequested) _asyncCameraAnimationCts.Cancel();

            var waiter = new WaitUntilCanceled();
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(waiter));

            if (_gamePlayManager.TryGetPlayerActorLastKnownPosition(out Vector3 playerActorPosition))
            {
                Vector3 position = _cameraOffset + playerActorPosition;
                Pal3.Instance.StartCoroutine(MoveAsync(position,
                    command.Duration,
                    AnimationCurveType.Sine,
                    () => waiter.CancelWait()));
            }
            else
            {
                _cameraFollowPlayer = true;
            }
        }
        #endif
    }
}