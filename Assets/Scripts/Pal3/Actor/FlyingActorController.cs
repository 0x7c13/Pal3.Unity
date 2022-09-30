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
    using MetaData;
    using Player;
    using Script.Waiter;
    using UnityEngine;

    public class FlyingActorController : MonoBehaviour,
        ICommandExecutor<FlyingActorFlyToCommand>
    {
        private const float FLY_SPEED = 144f / GameBoxInterpreter.GameBoxUnitToUnityUnit;
        
        private ActorActionController _actionController;

        public void Init(ActorActionController actionController)
        {
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
            Vector3 targetPosition = GameBoxInterpreter.ToUnityPosition(new Vector3(command.X, command.Y, command.Z));
            
            if (Vector3.Distance(transform.position, PlayerActorNpcInfo.InitPosition) < float.Epsilon)
            {
                transform.position = targetPosition;
                return;
            }

            var waiter = new WaitUntilCanceled(this);
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerWaitRequest(waiter));
            
            var distance = (targetPosition - transform.position).magnitude;
            var duration = distance / FLY_SPEED;

            _actionController.PerformAction(ActorActionType.Run);
            StartCoroutine(Fly(targetPosition, duration, waiter));
        }
        
        private IEnumerator Fly(Vector3 targetPosition, float duration, WaitUntilCanceled waiter = null)
        {
            Vector3 oldPosition = transform.position;
            
            // Facing towards target position, ignoring y
            transform.forward = (new Vector3(targetPosition.x, 0f, targetPosition.z)
                                 - new Vector3(oldPosition.x, 0f, oldPosition.z)).normalized;
            
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
            waiter?.CancelWait();
        }
    }
}