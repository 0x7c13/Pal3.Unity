// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Actor
{
    using System.Collections;
    using Command;
    using Command.SceCommands;
    using Core.GameBox;
    using Core.Services;
    using MetaData;
    using Scene;
    using Script.Waiter;
    using UnityEngine;

    public class HuaYingController : MonoBehaviour,
        ICommandExecutor<HuaYingFlyToCommand>,
        ICommandExecutor<HuaYingSwitchBehaviourModeCommand>
    {
        private const float HUAYING_FLY_SPEED = 144f / GameBoxInterpreter.GameBoxUnitToUnityUnit;
        private const float HUAYING_FOLLOW_TARGET_MIN_DISTANCE = 1f;
        private const float HUAYING_FOLLOW_TARGET_MAX_DISTANCE = 6f;
        private const float HUAYING_FOLLOW_TARGET_X_OFFSET = -0.8f;
        private const float HUAYING_FOLLOW_TARGET_Y_OFFSET = -0.8f;

        private readonly PlayerActorId _followActorId = PlayerActorId.XueJian;

        private SceneManager _sceneManager;
        private ActorController _actorController;
        private ActorActionController _actionController;

        private GameObject _target;
        private ActorController _targetActorController;
        private ActorActionController _targetActorActionController;
        private float _targetHeight;
        private bool _followTarget = true;

        public void Init(ActorController actorController,
            ActorActionController actionController)
        {
            _actorController = actorController;
            _actionController = actionController;
            _sceneManager = ServiceLocator.Instance.Get<SceneManager>();
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        private Vector3 GetTargetStayPosition()
        {
            var xOffsetDirection = Quaternion.Euler(0f, 90f, 0f) *_target.transform.forward;

            return _target.transform.position +
                   (-_target.transform.forward * HUAYING_FOLLOW_TARGET_MIN_DISTANCE) +
                   new Vector3(0f, _targetHeight + HUAYING_FOLLOW_TARGET_Y_OFFSET, 0f) +
                   xOffsetDirection .normalized * HUAYING_FOLLOW_TARGET_X_OFFSET;
        }

        private void LateUpdate()
        {
            if (!_followTarget) return;

            if (_target == null)
            {
                FindAndSetTarget();
            }

            if (!_targetActorController.IsActive && _actorController.IsActive)
            {
                _actorController.IsActive = false;
                return;
            }

            if (_targetActorController.IsActive && !_actorController.IsActive)
            {
                _actorController.IsActive = true;
                transform.position = GetTargetStayPosition();
                return;
            }

            if (!_actorController.IsActive) return;

            if (_targetHeight == 0f)
            {
                _targetHeight = _targetActorActionController.GetLocalBounds().size.y;
                transform.position = GetTargetStayPosition();
            }

            var myNewPosition = GetTargetStayPosition();

            if (Vector3.Distance(myNewPosition, transform.position) > HUAYING_FOLLOW_TARGET_MAX_DISTANCE)
            {
                transform.position = myNewPosition;
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position,
                    myNewPosition,
                    Time.deltaTime * HUAYING_FLY_SPEED);
            }

            transform.rotation = _target.transform.rotation;
        }

        private IEnumerator FlyTo(Vector3 position, float duration, WaitUntilCanceled waiter = null)
        {
            _actionController.PerformAction(ActorActionType.Run);
            var oldPosition = transform.position;

            var timePast = 0f;
            while (timePast < duration)
            {
                var newPosition = oldPosition;
                newPosition += (timePast / duration) * (position - oldPosition);

                transform.position = newPosition;

                timePast += Time.deltaTime;
                yield return null;
            }

            transform.position = position;
            // TODO: What about facing/rotation?
            waiter?.CancelWait();
        }

        private void FindAndSetTarget()
        {
            _target = _sceneManager.GetCurrentScene().GetActorGameObject((byte) _followActorId);
            _targetActorController = _target.GetComponent<ActorController>();
            _targetActorActionController = _target.GetComponent<ActorActionController>();
        }

        public void Execute(HuaYingFlyToCommand command)
        {
            var waiter = new WaitUntilCanceled(this);

            var position = GameBoxInterpreter.ToUnityPosition(new Vector3(command.X, command.Y, command.Z));
            var distance = (position - transform.position).magnitude;
            var duration = distance / HUAYING_FLY_SPEED;

            StartCoroutine(FlyTo(position, duration, waiter));
        }

        public void Execute(HuaYingSwitchBehaviourModeCommand command)
        {
            switch (command.Mode)
            {
                case 0:
                    _actorController.IsActive = false;
                    _followTarget = false;
                    break;
                case 1:
                    _actorController.IsActive = true;
                    _followTarget = true;
                    break;
                case 2:
                    _actorController.IsActive = true;
                    _followTarget = false;
                    break;
            }
        }
    }
}

#endif