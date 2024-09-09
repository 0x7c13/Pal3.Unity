// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
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
    using Engine.Animation;
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
    using Engine.Extensions;
    using Engine.Navigation;
    using Engine.Services;
    using Input;
    using GamePlay;
    using Script.Waiter;

    using Quaternion = UnityEngine.Quaternion;
    using Vector3 = UnityEngine.Vector3;

    public sealed class ActorController : GameEntityScript,
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
            Transform.Rotation = Quaternion.Euler(0, -_actor.Info.FacingDirection, 0);

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
                Vector3[] waypoints = new Vector3[_actor.Info.Path.NumberOfWaypoints];
                for (int i = 0; i < _actor.Info.Path.NumberOfWaypoints; i++)
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

        protected override void OnCollisionEnterGameEntity(IGameEntity otherGameEntity)
        {
            if (_actor.Info.Type == ActorType.CombatNpc &&
                (_actor.Info.MonsterIds[0] != 0 || _actor.Info.MonsterIds[1] != 0 || _actor.Info.MonsterIds[2] != 0) &&
                otherGameEntity.GetComponent<ActorController>() is {} actorController &&
                ServiceLocator.Instance.Get<PlayerActorManager>()
                    .GetPlayerActorId() == actorController.GetActor().Id)
            {
                Pal3.Instance.Execute(new CombatActorCollideWithPlayerActorNotification(
                        _actor.Id, actorController.GetActor().Id));
            }
        }

        private IEnumerator AnimateScaleAsync(float toScale, float duration, Action onFinished = null)
        {
            yield return CoreAnimation.EnumerateValueAsync(Transform.LocalScale.x,
                toScale, duration, AnimationCurveType.Linear, value =>
                {
                    Transform.LocalScale = new Vector3(value, value, value);
                });
            onFinished?.Invoke();
        }

        public void Execute(ActorSetFacingCommand command)
        {
            if (_actor.Id != command.ActorId) return;
            Transform.Rotation = Quaternion.Euler(0, command.Degrees, 0);
        }

        public void Execute(ActorRotateFacingCommand command)
        {
            if (_actor.Id != command.ActorId) return;
            #if PAL3
            float currentYAngles = Transform.EulerAngles.y;
            Transform.Rotation = Quaternion.Euler(0, currentYAngles - command.Degrees, 0);
            #elif PAL3A
            Transform.Rotation = Quaternion.Euler(0, - command.Degrees, 0);
            #endif
        }

        public void Execute(ActorSetFacingDirectionCommand command)
        {
            if (_actor.Id != command.ActorId) return;

            if (command.Direction is >= 0 and < 8)
            {
                Transform.Rotation = Quaternion.Euler(0, -command.Direction * 45, 0);
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
                Transform.Rotation = Quaternion.Euler(0, -command.Direction * 45, 0);
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
            WaitUntilCanceled waiter = new();
            Pal3.Instance.Execute(new ScriptRunnerAddWaiterRequest(waiter));
            StartCoroutine(AnimateScaleAsync(command.Scale, 2f, () => waiter.CancelWait()));
        }

        public void Execute(ActorSetYPositionCommand command)
        {
            if (command.ActorId != _actor.Id) return;
            Vector3 oldPosition = Transform.Position;
            Transform.Position = new Vector3(oldPosition.x,
                command.GameBoxYPosition.ToUnityYPosition(),
                oldPosition.z);
        }
    }
}