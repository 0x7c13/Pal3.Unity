// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Actor
{
    using Command;
    using Command.SceCommands;
    using Core.GameBox;
    using Core.Services;
    using MetaData;
    using Scene;
    using UnityEngine;

    public sealed class HuaYingController : FlyingActorController,
        ICommandExecutor<HuaYingSwitchBehaviourModeCommand>
    {
        private const float FLY_SPEED = 144f / GameBoxInterpreter.GameBoxUnitToUnityUnit;
        private const float FOLLOW_TARGET_MIN_DISTANCE = 1f;
        private const float FOLLOW_TARGET_MAX_DISTANCE = 6f;
        private const float FOLLOW_TARGET_X_OFFSET = -0.8f;
        private const float FOLLOW_TARGET_Y_OFFSET = -0.8f;

        private readonly PlayerActorId _followActorId = PlayerActorId.XueJian;

        private SceneManager _sceneManager;
        private ActorController _actorController;

        private bool _isTargetRegistered;
        private GameObject _target;
        private ActorController _targetActorController;
        private ActorActionController _targetActorActionController;
        private float _targetHeight;
        private bool _followTarget = true;

        public void Init(ActorController actorController,
            ActorActionController actionController)
        {
            base.Init(actionController);
            _actorController = actorController;
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
            var targetFacingDirection = _target.transform.forward;
            var xOffsetDirection = Quaternion.Euler(0f, 90f, 0f) * targetFacingDirection;

            return _target.transform.position +
                   (-targetFacingDirection * FOLLOW_TARGET_MIN_DISTANCE) +
                   new Vector3(0f, _targetHeight + FOLLOW_TARGET_Y_OFFSET, 0f) +
                   xOffsetDirection.normalized * FOLLOW_TARGET_X_OFFSET;
        }

        private void LateUpdate()
        {
            if (!_followTarget) return;

            if (!_isTargetRegistered)
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

            if (Vector3.Distance(myNewPosition, transform.position) > FOLLOW_TARGET_MAX_DISTANCE)
            {
                transform.position = myNewPosition;
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position,
                    myNewPosition,
                    Time.deltaTime * FLY_SPEED);
            }

            if (_isTargetRegistered)
            {
                transform.rotation = _target.transform.rotation;
            }
        }

        private void FindAndSetTarget()
        {
            _target = _sceneManager.GetCurrentScene().GetActorGameObject((byte) _followActorId);
            _targetActorController = _target.GetComponent<ActorController>();
            _targetActorActionController = _target.GetComponent<ActorActionController>();
            _isTargetRegistered = true;
        }
        
        public void Execute(HuaYingSwitchBehaviourModeCommand command)
        {
            switch (command.Mode)
            {
                case 0: // 隐藏
                    _actorController.IsActive = false;
                    _followTarget = false;
                    break;
                case 1: // 跟随雪见
                    _actorController.IsActive = true;
                    _followTarget = true;
                    break;
                case 2: // 单飞
                    _actorController.IsActive = true;
                    _followTarget = false;
                    break;
            }
        }
    }
}

#endif