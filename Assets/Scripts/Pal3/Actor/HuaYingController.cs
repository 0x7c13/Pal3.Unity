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
        private const float ROTATION_SPEED = 10f;
        private const float ROTATION_SYNCING_DISTANCE = 2f;
        private const float FOLLOW_TARGET_MIN_DISTANCE = 1f;
        private const float FOLLOW_TARGET_MAX_DISTANCE = 6f;
        private const float FOLLOW_TARGET_X_OFFSET = -0.8f;
        private const float FOLLOW_TARGET_Y_OFFSET = -0.8f;

        private const PlayerActorId FOLLOW_ACTOR_ID = PlayerActorId.XueJian;

        private SceneManager _sceneManager;
        private Actor _actor;
        private ActorController _actorController;
        private ActorActionController _actorActionController;

        private bool _isTargetRegistered;
        private GameObject _target;
        private ActorController _targetActorController;
        private ActorActionController _targetActorActionController;
        private float _targetHeight;
        private bool _followTarget = true;
        private int _currentMode = 1; // defaults to follow target actor

        public void Init(Actor actor,
            ActorController actorController,
            ActorActionController actionController)
        {
            base.Init(actor, actionController);
            _actor = actor;
            _actorController = actorController;
            _actorActionController = actionController;
            _sceneManager = ServiceLocator.Instance.Get<SceneManager>();
        }

        public int GetCurrentMode()
        {
            return _currentMode;
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
            Vector3 targetFacingDirection = _target.transform.forward;
            Vector3 xOffsetDirection = Quaternion.Euler(0f, 90f, 0f) * targetFacingDirection;

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

            Vector3 myNewPosition = GetTargetStayPosition();
            var distanceToNewPosition = Vector3.Distance(myNewPosition, transform.position);

            if (distanceToNewPosition < float.Epsilon)
            {
                _actorActionController.PerformAction(_actor.GetIdleAction());
            }
            else
            {
                if (Vector3.Distance(myNewPosition, transform.position) < FOLLOW_TARGET_MAX_DISTANCE)
                {
                    _actorActionController.PerformAction(_actor.GetMovementAction(0));
                }
                else
                {
                    transform.position = (transform.position - myNewPosition).normalized * FOLLOW_TARGET_MAX_DISTANCE +
                                         myNewPosition;
                    _actorActionController.PerformAction(_actor.GetMovementAction(1));
                }
                
                transform.position = Vector3.MoveTowards(transform.position,
                    myNewPosition,
                    Time.deltaTime * FLY_SPEED);
            }

            if (distanceToNewPosition < ROTATION_SYNCING_DISTANCE)
            {
                Vector3 newRotation= Vector3.RotateTowards(transform.forward,
                    _target.transform.forward, ROTATION_SPEED * Time.deltaTime, 0.0f);
                transform.rotation = Quaternion.LookRotation(newRotation);
            }
            else
            {
                transform.LookAt(new Vector3(myNewPosition.x, transform.position.y, myNewPosition.z));
            }
        }

        private void FindAndSetTarget()
        {
            _target = _sceneManager.GetCurrentScene().GetActorGameObject((byte) FOLLOW_ACTOR_ID);
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

            _currentMode = command.Mode;
        }
    }
}

#endif