// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Actor.Controllers
{
    using System;
    using System.Collections;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.GameBox;
    using Core.Navigation;
    using Script.Waiter;
    using UnityEngine;

    public class FlyingActorController : MonoBehaviour,
        ICommandExecutor<FlyingActorFlyToCommand>
    {
        public const float DefaultFlySpeed = 7.5f;
        public const float MaxFlySpeed = 11f;

        private const float FLYING_MOVEMENT_MODE_SWITCH_DISTANCE = 5f;
        private const float MAX_TARGET_DISTANCE = 20f;

        private ActorBase _actor;
        private ActorController _actorController;
        private ActorActionController _actionController;

        public void Init(ActorBase actor, ActorController actorController, ActorActionController actionController)
        {
            _actor = actor;
            _actorController = actorController;
            _actionController = actionController;
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Execute(FlyingActorFlyToCommand command)
        {
            Vector3 targetPosition = new Vector3(
                command.GameBoxXPosition,
                command.GameBoxYPosition,
                command.GameBoxZPosition).ToUnityPosition();

            // If actor is inactive or at it's init position, just teleport to target position
            if (!_actorController.IsActive ||
                Vector3.Distance(transform.position, NpcInfoFactory.ActorInitPosition) < float.Epsilon)
            {
                transform.position = targetPosition;
                return;
            }

            // In case the target position is too far away
            if (Vector3.Distance(transform.position, targetPosition) > MAX_TARGET_DISTANCE)
            {
                transform.position = (transform.position - targetPosition).normalized * MAX_TARGET_DISTANCE + targetPosition;
            }

            var waiter = new WaitUntilCanceled();
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(waiter));

            var distance = (targetPosition - transform.position).magnitude;
            var duration = distance / DefaultFlySpeed;

            _actionController.PerformAction(distance < FLYING_MOVEMENT_MODE_SWITCH_DISTANCE
                ? _actor.GetMovementAction(MovementMode.Walk)
                : _actor.GetMovementAction(MovementMode.Run));

            StartCoroutine(FlyToAsync(targetPosition, duration, () => waiter.CancelWait()));
        }

        private IEnumerator FlyToAsync(Vector3 targetPosition, float duration, Action onFinished = null)
        {
            Vector3 oldPosition = transform.position;

            // Facing towards target position, ignoring y
            transform.LookAt(new Vector3(targetPosition.x, oldPosition.y, targetPosition.z));

            var timePast = 0f;
            while (timePast < duration)
            {
                Vector3 newPosition = oldPosition;
                newPosition += (timePast / duration) * (targetPosition - oldPosition);

                transform.position = newPosition;

                timePast += Time.deltaTime;
                yield return null;
            }

            transform.position = targetPosition;
            _actionController.PerformAction(_actor.GetIdleAction());
            onFinished?.Invoke();
        }
    }
}