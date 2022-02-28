// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Camera
{
    using System;
    using System.Collections;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.Animation;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Core.Utils;
    using Input;
    using MetaData;
    using Player;
    using Scene;
    using Script.Waiter;
    using UnityEngine;
    using UnityEngine.InputSystem.OnScreen;
    using UnityEngine.UI;

    public class CameraManager : MonoBehaviour,
        ICommandExecutor<CameraSetTransformCommand>,
        ICommandExecutor<CameraSetDefaultTransformCommand>,
        ICommandExecutor<CameraShakeEffectCommand>,
        ICommandExecutor<CameraOrbitCommand>,
        ICommandExecutor<CameraRotateCommand>,
        ICommandExecutor<CameraFadeInCommand>,
        ICommandExecutor<CameraFadeInWhiteCommand>,
        ICommandExecutor<CameraFadeOutCommand>,
        ICommandExecutor<CameraFadeOutWhiteCommand>,
        ICommandExecutor<CameraPushCommand>,
        ICommandExecutor<CameraMoveCommand>,
        ICommandExecutor<CameraFreeCommand>,
        ICommandExecutor<CameraSetYawCommand>,
        ICommandExecutor<CameraFocusOnActorCommand>,
        ICommandExecutor<CameraFocusOnSceneObjectCommand>,
        ICommandExecutor<PlayerEnableInputCommand>,
        ICommandExecutor<ScenePreLoadingNotification>,
        ICommandExecutor<PlayerActorPositionUpdatedNotification>
    {
        private const float FADE_ANIMATION_DURATION = 3f;
        private const float SCENE_STORY_B_ROOM_HEIGHT = 32f;
        private const float CAMERA_ROTATION_SPEED_KEY_PRESS = 120f;
        private const float CAMERA_ROTATION_SPEED_SCROLL = 20f;
        private const float CAMERA_ROTATION_SPEED_DRAG = 10f;
        private const float CAMERA_SMOOTH_FOLLOW_TIME = 0.2f;
        private const float CAMERA_SMOOTH_FOLLOW_MAX_DISTANCE = 3f;

        private Camera _camera;
        private Image _curtainImage;
        private Vector3 _lastLookAtPoint = Vector3.zero;
        private Vector3 _cameraMoveVelocity = Vector3.zero;

        private bool _shouldResetVelocity = false;

        private Vector3 _actualPosition = Vector3.zero;
        private Vector3 _actualLookAtPoint = Vector3.zero;

        private Vector3 _cameraOffset = Vector3.zero;
        private float _lookAtPointYOffset;

        private bool _free = true;
        private bool _playerInputEnabled;

        private PlayerInputActions _inputActions;
        private PlayerManager _playerManager;
        private SceneManager _sceneManager;
        private bool _freeToRotate;

        private GameObject _lookAtGameObject;

        private RectTransform _joyStickRect;
        private float _joyStickMovementRange;
        private bool _isTouchEnabled;

        private bool _cameraAnimationInProgress;

        public void Init(PlayerInputActions inputActions,
            PlayerManager playerManager,
            SceneManager sceneManager,
            Camera mainCamera,
            Canvas touchControlUI,
            Image curtainImage)
        {
            _inputActions = inputActions;
            _playerManager = playerManager;
            _sceneManager = sceneManager;

            _camera = mainCamera;
            _camera!.fieldOfView = HorizontalToVerticalFov(24.05f, 4f/3f);

            _curtainImage = curtainImage;

            _isTouchEnabled = Utility.IsHandheldDevice();
            var onScreenStick = touchControlUI.GetComponentInChildren<OnScreenStick>();
            _joyStickMovementRange = onScreenStick.movementRange;
            var joyStickImage = onScreenStick.gameObject.GetComponent<Image>();
            _joyStickRect = joyStickImage.rectTransform;
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        private void LateUpdate()
        {
            if (!_playerInputEnabled && !_free)
            {
                _shouldResetVelocity = true;
                return;
            }

            if (_cameraAnimationInProgress) return;

            if (_lookAtGameObject != null)
            {
                var lookAtTransform = _lookAtGameObject.transform;
                var lookAtPosition = lookAtTransform.position;
                var cameraTransform = _camera.transform;
                cameraTransform.LookAt(lookAtTransform);
                cameraTransform.position = _cameraOffset + lookAtPosition;
                _lastLookAtPoint = lookAtPosition;
                _cameraOffset =  cameraTransform.position - lookAtPosition;
                return;
            }

            var yOffset = new Vector3(0f, _lookAtPointYOffset, 0f);
            var targetPosition = _lastLookAtPoint + _cameraOffset;
            var currentPosition = _camera.transform.position;

            var previousLookAtPoint = _lastLookAtPoint + (currentPosition - targetPosition);

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

            if (!_freeToRotate)
            {
                _camera.transform.LookAt(_actualLookAtPoint + yOffset);
                return;
            }

            RotateCameraBasedOnUserInput();
        }

        private void RotateCameraBasedOnUserInput()
        {
            if (_inputActions.Gameplay.RotateCameraClockwise.inProgress)
            {
                RotateToOrbitPoint(Time.deltaTime * CAMERA_ROTATION_SPEED_KEY_PRESS);
            }
            else if (_inputActions.Gameplay.RotateCameraCounterClockwise.inProgress)
            {
                RotateToOrbitPoint(-Time.deltaTime * CAMERA_ROTATION_SPEED_KEY_PRESS);
            }

            var mouseScroll = _inputActions.Gameplay.OnScroll.ReadValue<float>();
            if (mouseScroll != 0)
            {
                RotateToOrbitPoint(mouseScroll * Time.deltaTime * CAMERA_ROTATION_SPEED_SCROLL);
            }

            if (!_isTouchEnabled) return;

            var touch0Delta = _inputActions.Gameplay.Touch0Delta.ReadValue<float>();
            if (touch0Delta != 0)
            {
                var touchStartPosition = _inputActions.Gameplay.Touch0Start.ReadValue<Vector2>();
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _joyStickRect, touchStartPosition, null, out var localPoint);
                if (localPoint.x < -_joyStickMovementRange ||
                    localPoint.x > _joyStickRect.rect.width + _joyStickMovementRange ||
                    localPoint.y < -_joyStickMovementRange ||
                    localPoint.y > _joyStickRect.rect.height + _joyStickMovementRange)
                {
                    RotateToOrbitPoint(touch0Delta * Time.deltaTime * CAMERA_ROTATION_SPEED_DRAG);
                }
            }

            var touch1Delta = _inputActions.Gameplay.Touch1Delta.ReadValue<float>();
            if (touch1Delta != 0)
            {
                var touchStartPosition = _inputActions.Gameplay.Touch1Start.ReadValue<Vector2>();
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _joyStickRect, touchStartPosition, null, out var localPoint);
                if (localPoint.x < -_joyStickMovementRange ||
                    localPoint.x > _joyStickRect.rect.width + _joyStickMovementRange ||
                    localPoint.y < -_joyStickMovementRange ||
                    localPoint.y > _joyStickRect.rect.height + _joyStickMovementRange)
                {
                    RotateToOrbitPoint(touch1Delta * Time.deltaTime * CAMERA_ROTATION_SPEED_DRAG);
                }
            }
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

        private IEnumerator Shake(float duration, float amplitude, WaitUntilCanceled waiter = null)
        {
            _cameraAnimationInProgress = true;
            yield return AnimationHelper.ShakeTransform(_camera.transform,
                duration,
                amplitude,
                false,
                true,
                false);
            _cameraAnimationInProgress = false;
            waiter?.CancelWait();
        }

        private IEnumerator Move(Vector3 position, float duration, int mode, WaitUntilCanceled waiter = null)
        {
            _cameraAnimationInProgress = true;
            var curveType = (AnimationCurveType) mode;
            var cameraTransform = _camera.transform;
            var oldPosition = cameraTransform.position;
            yield return AnimationHelper.MoveTransform(cameraTransform, position, duration, curveType);
            _lastLookAtPoint += position - oldPosition;
            _cameraOffset = _camera.transform.position - _lastLookAtPoint;
            _cameraAnimationInProgress = false;
            waiter?.CancelWait();
        }

        private IEnumerator Orbit(Quaternion toRotation,
            float duration,
            AnimationCurveType curveType,
            WaitUntilCanceled waiter = null)
        {
            _cameraAnimationInProgress = true;
            var lookAtPoint = _lastLookAtPoint;
            yield return AnimationHelper.OrbitTransformAroundCenterPoint(_camera.transform,
                toRotation,
                lookAtPoint,
                duration,
                curveType);
            _cameraOffset = _camera.transform.position - lookAtPoint;
            _cameraAnimationInProgress = false;
            waiter?.CancelWait();
        }

        private IEnumerator Rotate(Quaternion toRotation,
            float duration,
            AnimationCurveType curveType,
            WaitUntilCanceled waiter = null)
        {
            _cameraAnimationInProgress = true;
            yield return AnimationHelper.RotateTransform(_camera.transform, toRotation, duration, curveType);
            _cameraAnimationInProgress = false;
            waiter?.CancelWait();
        }

        public IEnumerator Push(float distance,
            float duration,
            AnimationCurveType curveType,
            WaitUntilCanceled waiter = null)
        {
            _cameraAnimationInProgress = true;
            var oldPosition = _camera.transform.position;
            var oldDistance = Vector3.Distance(oldPosition, _lastLookAtPoint);
            var cameraDirection = (oldPosition - _lastLookAtPoint).normalized;
            var newPosition = oldPosition + cameraDirection *(distance - oldDistance);
            var cameraTransform = _camera.transform;
            yield return AnimationHelper.MoveTransform(cameraTransform, newPosition, duration, curveType);
            _cameraOffset = cameraTransform.position - _lastLookAtPoint;
            _cameraAnimationInProgress = false;
            waiter?.CancelWait();
        }

        public IEnumerator Fade(bool fadeIn, float duration, Color color, WaitUntilCanceled waiter = null)
        {
            _curtainImage.color = color;

            float from = 1f, to = 0f;
            if (!fadeIn) { from = 0f; to = 1f; }

            yield return AnimationHelper.EnumerateValue(from, to, duration, AnimationCurveType.Linear,
                alpha =>
            {
                _curtainImage.color = new Color(color.r, color.g, color.b, alpha);
            });

            _curtainImage.color = new Color(color.r, color.g, color.b, to);

            waiter?.CancelWait();
        }

        public void ApplySceneSettings(ScnSceneInfo sceneInfo)
        {
            switch (sceneInfo.SceneType)
            {
                case ScnSceneType.StoryB:
                    _freeToRotate = false;
                    _lookAtPointYOffset = SCENE_STORY_B_ROOM_HEIGHT /
                                          GameBoxInterpreter.GameBoxUnitToUnityUnit;
                    ApplyDefaultSettings(1);
                    break;
                case ScnSceneType.StoryA:
                    _freeToRotate = true;
                    _lookAtPointYOffset = 0;
                    ApplyDefaultSettings(0);
                    break;
                case ScnSceneType.Maze:
                    _freeToRotate = true;
                    _lookAtPointYOffset = 0;
                    ApplyDefaultSettings(0);
                    break;
                default:
                    _lookAtPointYOffset = 0;
                    ApplyDefaultSettings(0);
                    break;
            }

            _shouldResetVelocity = true;
        }

        public void ApplyDefaultSettings(int option)
        {
            Quaternion rotation;
            float distance = 688.0f / GameBoxInterpreter.GameBoxUnitToUnityUnit;

            switch (option)
            {
                case 0:
                    _camera.fieldOfView = HorizontalToVerticalFov(26.0f, 4f/3f);
                    distance = 1144 / GameBoxInterpreter.GameBoxUnitToUnityUnit;
                    rotation = GameBoxInterpreter.ToUnityRotation(-30.37f, -52.65f, 0f);
                    break;
                case 1:
                    _camera.fieldOfView = HorizontalToVerticalFov(24.05f, 4f/3f);
                    rotation = GameBoxInterpreter.ToUnityRotation(-19.48f, 33.24f, 0f);
                    break;
                case 2:
                    _camera.fieldOfView = HorizontalToVerticalFov(24.05f, 4f/3f);
                    rotation = GameBoxInterpreter.ToUnityRotation(-19.48f, -33.24f, 0f);
                    break;
                case 3:
                    _camera.fieldOfView = HorizontalToVerticalFov(24.05f, 4f/3f);
                    rotation = GameBoxInterpreter.ToUnityRotation(-19.48f, 0f, 0f);
                    break;
                default:
                    return;
            }

            var yOffset = new Vector3(0f, _lookAtPointYOffset, 0f);

            var cameraDirection = (rotation * Vector3.forward).normalized;
            var cameraPosition = _lastLookAtPoint + cameraDirection * -distance + yOffset;

            var cameraTransform = _camera.transform;
            cameraTransform.rotation = rotation;
            cameraTransform.position = cameraPosition;

            _cameraOffset = cameraTransform.position - _lastLookAtPoint;
        }

        public void Execute(PlayerActorPositionUpdatedNotification notification)
        {
            _lastLookAtPoint = notification.Position;
        }

        public void Execute(CameraSetDefaultTransformCommand command)
        {
            ApplyDefaultSettings(command.Option);
            _free = true;
        }

        public void Execute(CameraSetTransformCommand command)
        {
            _lookAtGameObject = null;

            var cameraPosition = GameBoxInterpreter.ToUnityPosition(
                new Vector3(command.X, command.Y, command.Z));
            var cameraTransform = _camera.transform;
            cameraTransform.position = cameraPosition;
            cameraTransform.rotation = GameBoxInterpreter.ToUnityRotation(command.Pitch, command.Yaw, 0f);

            _lastLookAtPoint = cameraTransform.position + cameraTransform.forward *
                (command.Distance / GameBoxInterpreter.GameBoxUnitToUnityUnit);
            _cameraOffset = cameraPosition - _lastLookAtPoint;
        }

        public void Execute(CameraFreeCommand command)
        {
            _lookAtGameObject = null;
            _free = command.Free == 1;
        }

        public void Execute(CameraShakeEffectCommand command)
        {
            var waiter = new WaitUntilCanceled(this);
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerWaitRequest(waiter));
            StartCoroutine(Shake(command.Duration,
                command.Amplitude / GameBoxInterpreter.GameBoxUnitToUnityUnit, waiter));
        }

        public void Execute(CameraOrbitCommand command)
        {
            var waiter = new WaitUntilCanceled(this);
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerWaitRequest(waiter));
            var rotation = GameBoxInterpreter.ToUnityRotation(command.Pitch, command.Yaw, 0f);
            StartCoroutine(Orbit(rotation, command.Duration, (AnimationCurveType)command.CurveType, waiter));
        }

        public void Execute(CameraRotateCommand command)
        {
            var waiter = new WaitUntilCanceled(this);
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerWaitRequest(waiter));
            var rotation = GameBoxInterpreter.ToUnityRotation(command.Pitch, command.Yaw, 0f);
            StartCoroutine(Rotate(rotation, command.Duration, (AnimationCurveType)command.CurveType, waiter));
        }

        public void Execute(CameraFadeInCommand command)
        {
            var waiter = new WaitUntilCanceled(this);
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerWaitRequest(waiter));
            StartCoroutine(Fade(true, FADE_ANIMATION_DURATION, Color.black, waiter));
        }

        public void Execute(CameraFadeInWhiteCommand command)
        {
            var waiter = new WaitUntilCanceled(this);
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerWaitRequest(waiter));
            StartCoroutine(Fade(true, FADE_ANIMATION_DURATION, Color.white, waiter));
        }

        public void Execute(CameraFadeOutCommand command)
        {
            var waiter = new WaitUntilCanceled(this);
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerWaitRequest(waiter));
            StartCoroutine(Fade(false, FADE_ANIMATION_DURATION, Color.black, waiter));
        }

        public void Execute(CameraFadeOutWhiteCommand command)
        {
            var waiter = new WaitUntilCanceled(this);
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerWaitRequest(waiter));
            StartCoroutine(Fade(false, FADE_ANIMATION_DURATION, Color.white, waiter));
        }

        public void Execute(CameraPushCommand command)
        {
            var waiter = new WaitUntilCanceled(this);
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerWaitRequest(waiter));
            var distance = command.Distance / GameBoxInterpreter.GameBoxUnitToUnityUnit;
            StartCoroutine(Push(distance, command.Duration, (AnimationCurveType)command.CurveType, waiter));
        }

        public void Execute(CameraMoveCommand command)
        {
            var waiter = new WaitUntilCanceled(this);
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerWaitRequest(waiter));
            var position = GameBoxInterpreter.ToUnityPosition(new Vector3(command.X, command.Y, command.Z));
            StartCoroutine(Move(position, command.Duration, command.Mode, waiter));
        }

        public void Execute(CameraSetYawCommand command)
        {
            RotateToOrbitPoint(command.Yaw);
        }

        public void Execute(ScenePreLoadingNotification notification)
        {
            ApplySceneSettings(notification.SceneInfo);
        }

        public void Execute(PlayerEnableInputCommand command)
        {
            _playerInputEnabled = (command.Enable == 1);
        }

        public void Execute(CameraFocusOnActorCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID) return;
            _lookAtGameObject = _sceneManager.GetCurrentScene().GetActorGameObject((byte)command.ActorId);
        }

        public void Execute(CameraFocusOnSceneObjectCommand command)
        {
            _lookAtGameObject = _sceneManager.GetCurrentScene()
                .GetSceneObjectGameObject((byte)command.SceneObjectId);
        }
    }
}