// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Actor.Controllers
{
    using System;
    using System.Collections;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Engine.Abstraction;
    using Engine.Animation;
    using Engine.Extensions;
    using Engine.Navigation;
    using Engine.Services;
    using Input;
    using GamePlay;
    using Script.Waiter;
    using UnityEngine;

    public class ActorController : GameEntityBase,
        ICommandExecutor<ActorSetFacingCommand>,
        ICommandExecutor<ActorSetFacingDirectionCommand>,
        ICommandExecutor<ActorRotateFacingCommand>,
        ICommandExecutor<ActorRotateFacingDirectionCommand>,
        ICommandExecutor<ActorSetScriptCommand>,
        ICommandExecutor<ActorSetYPositionCommand>,
        ICommandExecutor<ActorChangeScaleCommand>
    {
        private Actor _actor;
        private ActorActionController _actionController;
        private ActorMovementController _movementController;
        private PlayerInputActions _inputActions;

        public bool IsActive
        {
            get => _actor.IsActive;
            set
            {
                if (value) Activate();
                else DeActivate();

                _actor.IsActive = value;
            }
        }

        private ActorBehaviourType _currentBehaviour;

        protected override void OnEnableGameEntity()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        protected override void OnDisableGameEntity()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Init(Actor actor,
            ActorActionController actionController,
            ActorMovementController movementController)
        {
            _actor = actor;
            _actionController = actionController;
            _movementController = movementController;

            // Init facing direction
            transform.rotation = Quaternion.Euler(0, -_actor.Info.FacingDirection, 0);

            // Activate if InitActive == 1
            if (_actor.Info.InitActive == 1) IsActive = true;
        }

        public ActorBehaviourType GetCurrentBehaviour()
        {
            return _currentBehaviour;
        }

        public bool IsDirectlyInteractable(float distance)
        {
            if (distance > _actor.GetInteractionMaxDistance()) return false;
            return _actor.GetScriptId() != ScriptConstants.InvalidScriptId;
        }

        public Actor GetActor()
        {
            return _actor;
        }

        private void Activate()
        {
            #if PAL3A
            // Reset NanGongHuang actor name
            if (_actor.Id == (int)PlayerActorId.NanGongHuang &&
                !string.Equals(_actor.Name, ActorConstants.NanGongHuangHumanModeActorName))
            {
                _actor.ChangeName(ActorConstants.NanGongHuangHumanModeActorName);
            }
            #endif

            if (_actor.Info.Type == ActorType.MainActor)
            {
                if (string.IsNullOrEmpty(_actionController.GetCurrentAction()))
                {
                    _actionController.PerformAction(_actor.GetIdleAction());
                }
                return;
            }

            // Perform init action
            if (!_actor.IsActive)
            {
                switch (_actor.Info.InitBehaviour)
                {
                    case ActorBehaviourType.None:
                        _currentBehaviour = ActorBehaviourType.None;
                        _actionController.PerformAction(_actor.GetIdleAction());
                        break;
                    case ActorBehaviourType.Hold:
                        _currentBehaviour = ActorBehaviourType.Hold;
                        _actionController.PerformAction(_actor.GetInitAction());
                        if (_actor.Info.LoopAction == 0)
                        {
                            _actionController.PauseAnimation(); // Hold at the first frame
                        }
                        break;
                    case ActorBehaviourType.Wander:
                        _currentBehaviour = ActorBehaviourType.Wander;
                        _actionController.PerformAction(_actor.GetIdleAction());
                        break;
                    case ActorBehaviourType.PathFollow:
                        _currentBehaviour = ActorBehaviourType.PathFollow;
                        _actionController.PerformAction(_actor.GetIdleAction());
                        break;
                }
            }

            if (_currentBehaviour == ActorBehaviourType.PathFollow &&
                _actor.Info.Path.NumberOfWaypoints > 0)
            {
                var waypoints = new Vector3[_actor.Info.Path.NumberOfWaypoints];
                for (var i = 0; i < _actor.Info.Path.NumberOfWaypoints; i++)
                {
                    waypoints[i] = _actor.Info.Path.GameBoxWaypoints[i].ToUnityPosition();
                }
                _movementController.SetupPath(waypoints, 0, EndOfPathActionType.WaitAndReverse, ignoreObstacle: true);
            }
        }

        private void DeActivate()
        {
            _actionController.DeActivate();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_actor.Info.Type == ActorType.CombatNpc &&
                (_actor.Info.MonsterIds[0] != 0 || _actor.Info.MonsterIds[1] != 0 || _actor.Info.MonsterIds[2] != 0) &&
                collision.gameObject.GetComponent<ActorController>() is {} actorController &&
                (int) ServiceLocator.Instance.Get<PlayerActorManager>()
                    .GetPlayerActor() == actorController.GetActor().Id)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new CombatActorCollideWithPlayerActorNotification(
                        _actor.Id, actorController.GetActor().Id));
            }
        }

        private IEnumerator AnimateScaleAsync(float toScale, float duration, Action onFinished = null)
        {
            yield return CoreAnimation.EnumerateValueAsync(transform.localScale.x,
                toScale, duration, AnimationCurveType.Linear, value =>
                {
                    transform.localScale = new Vector3(value, value, value);
                });
            onFinished?.Invoke();
        }

        public void Execute(ActorSetFacingCommand command)
        {
            if (_actor.Id != command.ActorId) return;
            transform.rotation = Quaternion.Euler(0, command.Degrees, 0);
        }

        public void Execute(ActorRotateFacingCommand command)
        {
            if (_actor.Id != command.ActorId) return;
            #if PAL3
            var currentYAngles = transform.rotation.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0, currentYAngles - command.Degrees, 0);
            #elif PAL3A
            transform.rotation = Quaternion.Euler(0, - command.Degrees, 0);
            #endif
        }

        public void Execute(ActorSetFacingDirectionCommand command)
        {
            if (_actor.Id != command.ActorId) return;

            if (command.Direction is >= 0 and < 8)
            {
                transform.rotation = Quaternion.Euler(0, -command.Direction * 45, 0);
            }
            else
            {
                // Note: in the original game scripts, this command is sometimes misused to set the facing direction.
                // Instead of calling ActorRotateFacingCommand, there are places where the script calls
                // ActorSetFacingDirectionCommand. In this case, the direction parameter is actually the degrees
                // to rotate. Thus we need to handle this case here.
                int degrees = command.Direction;
                Execute(new ActorRotateFacingCommand(command.ActorId, degrees));
            }
        }

        public void Execute(ActorRotateFacingDirectionCommand command)
        {
            if (_actor.Id != command.ActorId) return;

            if (command.Direction is >= 0 and < 8)
            {
                transform.rotation = Quaternion.Euler(0, -command.Direction * 45, 0);
            }
            else
            {
                // Note: in the original game scripts, this command is sometimes misused to set the facing direction.
                // Instead of calling ActorRotateFacingCommand, there are places where the script calls
                // ActorRotateFacingDirectionCommand. In this case, the direction parameter is actually the degrees
                // to rotate. Thus we need to handle this case here.
                int degrees = command.Direction;
                Execute(new ActorRotateFacingCommand(command.ActorId, degrees));
            }
        }

        public void Execute(ActorSetScriptCommand command)
        {
            if (command.ActorId != _actor.Id) return;
            if (_actor.GetScriptId() != (uint)command.ScriptId)
            {
                _actor.ChangeScriptId((uint)command.ScriptId);
            }
        }

        public void Execute(ActorChangeScaleCommand command)
        {
            if (command.ActorId != _actor.Id) return;
            var waiter = new WaitUntilCanceled();
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(waiter));
            StartCoroutine(AnimateScaleAsync(command.Scale, 2f, () => waiter.CancelWait()));
        }

        public void Execute(ActorSetYPositionCommand command)
        {
            if (command.ActorId != _actor.Id) return;
            Vector3 oldPosition = transform.position;
            transform.position = new Vector3(oldPosition.x,
                command.GameBoxYPosition.ToUnityYPosition(),
                oldPosition.z);
        }
    }
}