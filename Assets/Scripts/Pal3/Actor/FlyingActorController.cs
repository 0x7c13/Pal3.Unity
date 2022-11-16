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
    using Core.GameBox;
    using Script.Waiter;
    using UnityEngine;

    public class FlyingActorController : MonoBehaviour,
        ICommandExecutor<FlyingActorFlyToCommand>
    {
        private const float FLY_SPEED = 144f / GameBoxInterpreter.GameBoxUnitToUnityUnit;
        private const float FLYING_MOVEMENT_MODE_SWITCH_DISTANCE = 5f;
        private const float MAX_TARGET_DISTANCE = 10f;
        
        private Actor _actor;
        private ActorActionController _actionController;

        public void Init(Actor actor, ActorActionController actionController)
        {
            _actor = actor;
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
            Vector3 targetPosition = GameBoxInterpreter.ToUnityPosition(new Vector3(
                command.GameBoxXPosition,
                command.GameBoxYPosition,
                command.GameBoxZPosition));
            
            // In case the target position is too far away
            if (Vector3.Distance(transform.position, targetPosition) > MAX_TARGET_DISTANCE)
            {
                transform.position = (targetPosition - transform.position).normalized * MAX_TARGET_DISTANCE + targetPosition;
            }

            var waiter = new WaitUntilCanceled(this);
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerWaitRequest(waiter));
            
            var distance = (targetPosition - transform.position).magnitude;
            var duration = distance / FLY_SPEED;

            _actionController.PerformAction(distance < FLYING_MOVEMENT_MODE_SWITCH_DISTANCE
                ? _actor.GetMovementAction(0)
                : _actor.GetMovementAction(1));

            StartCoroutine(Fly(targetPosition, duration, waiter));
        }
        
        private IEnumerator Fly(Vector3 targetPosition, float duration, WaitUntilCanceled waiter = null)
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
            waiter?.CancelWait();
        }
    }
}