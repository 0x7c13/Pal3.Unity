// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Actor
{
    using System.Collections;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.Animation;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Input;
    using MetaData;
    using Script.Waiter;
    using UnityEngine;

    public class ActorController : MonoBehaviour,
        ICommandExecutor<ActorSetFacingDirectionCommand>,
        ICommandExecutor<ActorRotateFacingCommand>,
        ICommandExecutor<ActorRotateFacingDirectionCommand>,
        ICommandExecutor<ActorSetScriptCommand>,
        #if PAL3A
        ICommandExecutor<ActorSetYPositionCommand>,
        #endif
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
                if (value)
                {
                    Activate();
                }
                else
                {
                    DeActivate();
                }

                _actor.IsActive = value;
            }
        }

        private ScnActorBehaviour _currentBehaviour;

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

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public bool IsInteractable(float distance)
        {
            var maxInteractionDistance = _actor.GetInteractionMaxDistance();
            if (distance > maxInteractionDistance) return false;
            return _actor.Info.ScriptId != 0;
        }

        private void Activate()
        {
            #if PAL3A
            // Reset NanGongHuang actor name
            if (_actor.Info.Id == (byte)PlayerActorId.NanGongHuang &&
                !string.Equals(_actor.Info.Name, ActorConstants.NanGongHuangHumanModeActorName))
            {
                _actor.ChangeName(ActorConstants.NanGongHuangHumanModeActorName);
            }
            #endif
            
            if (_actor.Info.Kind == ScnActorKind.MainActor)
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
                    case ScnActorBehaviour.None:
                        _currentBehaviour = ScnActorBehaviour.None;
                        _actionController.PerformAction(_actor.GetIdleAction());
                        break;
                    case ScnActorBehaviour.Hold:
                        _currentBehaviour = ScnActorBehaviour.Hold;
                        _actionController.PerformAction(_actor.GetInitAction());
                        break;
                    case ScnActorBehaviour.Wander:
                        _currentBehaviour = ScnActorBehaviour.Wander;
                        _actionController.PerformAction(_actor.GetIdleAction());
                        break;
                    case ScnActorBehaviour.PathFollow:
                        _currentBehaviour = ScnActorBehaviour.PathFollow;
                        _actionController.PerformAction(_actor.GetIdleAction());
                        break;
                }
            }

            if (_currentBehaviour == ScnActorBehaviour.PathFollow &&
                _actor.Info.Path.NumberOfWaypoints > 0)
            {
                var waypoints = new Vector3[_actor.Info.Path.NumberOfWaypoints];
                for (var i = 0; i < _actor.Info.Path.NumberOfWaypoints; i++)
                {
                    waypoints[i] = GameBoxInterpreter.ToUnityPosition(_actor.Info.Path.GameBoxWaypoints[i]);
                }
                _movementController.SetupPath(waypoints, 0, EndOfPathActionType.Reverse, ignoreObstacle: true);
            }
        }

        private void DeActivate()
        {
            _actionController.DeActivate();
        }

        private IEnumerator AnimateScale(float toScale, float duration, WaitUntilCanceled waiter = null)
        {
            yield return AnimationHelper.EnumerateValue(transform.localScale.x,
                toScale, duration, AnimationCurveType.Linear, value =>
                {
                    transform.localScale = new Vector3(value, value, value);
                });
            waiter?.CancelWait();
        }

        public void Execute(ActorRotateFacingCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            #if PAL3
            var currentYAngles = transform.rotation.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0, currentYAngles - command.Degrees, 0);
            #elif PAL3A
            transform.rotation = Quaternion.Euler(0, - command.Degrees, 0);
            #endif
        }

        public void Execute(ActorSetFacingDirectionCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            transform.rotation = Quaternion.Euler(0, -command.Direction * 45, 0);
        }

        public void Execute(ActorRotateFacingDirectionCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            transform.rotation = Quaternion.Euler(0, -command.Direction * 45, 0);
        }

        public void Execute(ActorSetScriptCommand command)
        {
            if (command.ActorId != _actor.Info.Id) return;
            _actor.Info.ScriptId = (uint)command.ScriptId;
        }

        public void Execute(ActorChangeScaleCommand command)
        {
            if (command.ActorId != _actor.Info.Id) return;
            var waiter = new WaitUntilCanceled(this);
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ScriptRunnerWaitRequest(waiter));
            StartCoroutine(AnimateScale(command.Scale, 2f, waiter));
        }

        #if PAL3A
        public void Execute(ActorSetYPositionCommand command)
        {
            if (command.ActorId != _actor.Info.Id) return;
            Vector3 oldPosition = transform.position;
            transform.position = new Vector3(oldPosition.x,
                GameBoxInterpreter.ToUnityYPosition(command.GameBoxYPosition),
                oldPosition.z);
        }
        #endif
    }
}