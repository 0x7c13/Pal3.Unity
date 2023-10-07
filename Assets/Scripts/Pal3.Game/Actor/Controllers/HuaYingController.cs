// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Game.Actor.Controllers
{
    using Command;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Enums;
    using Engine.Abstraction;
    using Engine.Services;
    using Scene;
    using UnityEngine;

    public sealed class HuaYingController : FlyingActorController,
        ICommandExecutor<HuaYingSwitchBehaviourModeCommand>
    {
        private const float MAX_FLY_SPEED = 11f;
        private const float ROTATION_SPEED = 10f;
        private const float ROTATION_SYNCING_DISTANCE = 2f;
        private const float FOLLOW_TARGET_MIN_DISTANCE = 1f;
        private const float FOLLOW_TARGET_FLY_SPEED_CHANGE_DISTANCE = 8f;
        private const float FOLLOW_TARGET_MAX_DISTANCE = 15f;
        private const float FOLLOW_TARGET_X_OFFSET = 0.8f;
        private const float FOLLOW_TARGET_Y_OFFSET = -0.8f;

        private const PlayerActorId FOLLOW_ACTOR_ID = PlayerActorId.XueJian;

        private SceneManager _sceneManager;
        private ActorBase _actor;
        private ActorController _actorController;
        private ActorActionController _actorActionController;

        private bool _isTargetRegistered;
        private IGameEntity _target;
        private ActorController _targetActorController;
        private ActorActionController _targetActorActionController;
        private float _targetHeight;
        private bool _followTarget = true;
        private int _currentMode = 1; // defaults to follow target actor

        protected override void OnEnableGameEntity()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        protected override void OnDisableGameEntity()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public new void Init(ActorBase actor,
            ActorController actorController,
            ActorActionController actionController)
        {
            base.Init(actor, actorController, actionController);
            _actor = actor;
            _actorController = actorController;
            _actorActionController = actionController;
            _sceneManager = ServiceLocator.Instance.Get<SceneManager>();
        }

        public int GetCurrentMode()
        {
            return _currentMode;
        }

        private Vector3 GetTargetStayPosition()
        {
            Vector3 targetFacingDirection = _target.Transform.Forward;
            Vector3 xOffsetDirection = Quaternion.Euler(0f, 90f, 0f) * targetFacingDirection;

            return _target.Transform.Position +
                   (-targetFacingDirection * FOLLOW_TARGET_MIN_DISTANCE) +
                   new Vector3(0f, _targetHeight + FOLLOW_TARGET_Y_OFFSET, 0f) +
                   xOffsetDirection.normalized * FOLLOW_TARGET_X_OFFSET;
        }

        protected override void OnLateUpdateGameEntity(float deltaTime)
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
                Transform.Position = GetTargetStayPosition();
                return;
            }

            if (!_actorController.IsActive) return;

            if (_targetHeight == 0f)
            {
                _targetHeight = _targetActorActionController.GetMeshBounds().size.y;
                Transform.Position = GetTargetStayPosition();
            }

            Vector3 myNewPosition = GetTargetStayPosition();
            var distanceToNewPosition = Vector3.Distance(myNewPosition, Transform.Position);

            if (distanceToNewPosition < float.Epsilon)
            {
                _actorActionController.PerformAction(_actor.GetIdleAction());
            }
            else
            {
                // Set max distance to follow target if it's too far away
                var distanceToTarget = Vector3.Distance(myNewPosition, Transform.Position);
                if (distanceToTarget > FOLLOW_TARGET_MAX_DISTANCE)
                {
                    Transform.Position = (Transform.Position - myNewPosition).normalized * FOLLOW_TARGET_MAX_DISTANCE +
                                         myNewPosition;
                }

                // Increase fly speed if the distance is greater than a threshold
                var flySpeed = distanceToTarget > FOLLOW_TARGET_FLY_SPEED_CHANGE_DISTANCE ? MaxFlySpeed : DefaultFlySpeed;

                _actorActionController.PerformAction(
                    Vector3.Distance(myNewPosition, Transform.Position) < FOLLOW_TARGET_FLY_SPEED_CHANGE_DISTANCE - 1f
                        ? _actor.GetMovementAction(MovementMode.Walk)
                        : _actor.GetMovementAction(MovementMode.Run));

                Transform.Position = Vector3.MoveTowards(Transform.Position,
                    myNewPosition,
                    deltaTime * flySpeed);
            }

            if (distanceToNewPosition < ROTATION_SYNCING_DISTANCE)
            {
                Vector3 newRotation = Vector3.RotateTowards(Transform.Forward,
                    _target.Transform.Forward, ROTATION_SPEED * deltaTime, 0.0f);
                Transform.Rotation = Quaternion.LookRotation(newRotation);
            }
            else
            {
                Transform.LookAt(new Vector3(myNewPosition.x, Transform.Position.y, myNewPosition.z));
            }
        }

        private void FindAndSetTarget()
        {
            _target = _sceneManager.GetCurrentScene().GetActorGameEntity((int) FOLLOW_ACTOR_ID);
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