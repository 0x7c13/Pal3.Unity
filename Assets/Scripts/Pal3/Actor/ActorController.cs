// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Actor
{
    using System;
    using System.Collections;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.Animation;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Core.Services;
    using Input;
    using MetaData;
    using Player;
    using Scene;
    using Script.Waiter;
    using UnityEngine;

    public class ActorController : MonoBehaviour,
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

        private bool _isScriptChanged;

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

        public ScnActorBehaviour GetCurrentBehaviour()
        {
            return _currentBehaviour;
        }

        public bool IsDirectlyInteractable(float distance)
        {
            if (distance > _actor.GetInteractionMaxDistance()) return false;
            return _actor.Info.ScriptId != ScriptConstants.InvalidScriptId;
        }

        public Actor GetActor()
        {
            return _actor;
        }

        public bool IsScriptChanged()
        {
            return _isScriptChanged;
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
                _movementController.SetupPath(waypoints, 0, EndOfPathActionType.WaitAndReverse, ignoreObstacle: true);
            }
        }

        private void DeActivate()
        {
            _actionController.DeActivate();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_actor.Info.Kind == ScnActorKind.CombatNpc &&
                ServiceLocator.Instance.Get<SceneManager>()
                    .GetCurrentScene()
                    .GetSceneInfo().SceneType == ScnSceneType.Maze &&
                collision.gameObject.GetComponent<ActorController>() is {} actorController &&
                (byte) ServiceLocator.Instance.Get<PlayerManager>()
                    .GetPlayerActor() == actorController.GetActor().Info.Id)
            {
                // Player actor collides with combat NPC in maze
                // TODO: Implement combat
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new PlaySfxCommand("wd130", 1));
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorActivateCommand(_actor.Info.Id, 0));
            }
        }

        private IEnumerator AnimateScaleAsync(float toScale, float duration, WaitUntilCanceled waiter = null)
        {
            yield return AnimationHelper.EnumerateValueAsync(transform.localScale.x,
                toScale, duration, AnimationCurveType.Linear, value =>
                {
                    transform.localScale = new Vector3(value, value, value);
                });
            waiter?.CancelWait();
        }

        public void Execute(ActorSetFacingCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            transform.rotation = Quaternion.Euler(0, command.Degrees, 0);
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
            if (_actor.Info.Id != command.ActorId) return;

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
            if (command.ActorId != _actor.Info.Id) return;
            if (_actor.Info.ScriptId != (uint) command.ScriptId)
            {
                _isScriptChanged = true;
                _actor.Info.ScriptId = (uint) command.ScriptId;
            }
        }

        public void Execute(ActorChangeScaleCommand command)
        {
            if (command.ActorId != _actor.Info.Id) return;
            var waiter = new WaitUntilCanceled();
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ScriptRunnerAddWaiterRequest(waiter));
            StartCoroutine(AnimateScaleAsync(command.Scale, 2f, waiter));
        }

        public void Execute(ActorSetYPositionCommand command)
        {
            if (command.ActorId != _actor.Info.Id) return;
            Vector3 oldPosition = transform.position;
            transform.position = new Vector3(oldPosition.x,
                GameBoxInterpreter.ToUnityYPosition(command.GameBoxYPosition),
                oldPosition.z);
        }
    }
}