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
    using Core.Contract.Enums;
    using Core.Primitives;
    using Engine.Abstraction;
    using Engine.Extensions;
    using Engine.Services;
    using Script.Waiter;

    using Vector3 = UnityEngine.Vector3;

    public class FlyingActorController : TickableGameEntityScript,
        ICommandExecutor<FlyingActorFlyToCommand>
    {
        public const float DefaultFlySpeed = 7.5f;
        public const float MaxFlySpeed = 11f;

        private const float FLYING_MOVEMENT_MODE_SWITCH_DISTANCE = 5f;
        private const float MAX_TARGET_DISTANCE = 20f;

        private IGameTimeProvider _gameTimeProvider;

        private ActorBase _actor;
        private ActorController _actorController;
        private ActorActionController _actionController;

        protected override void OnEnableGameEntity()
        {
            _gameTimeProvider = ServiceLocator.Instance.Get<IGameTimeProvider>();
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        protected override void OnDisableGameEntity()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Init(ActorBase actor, ActorController actorController, ActorActionController actionController)
        {
            _actor = actor;
            _actorController = actorController;
            _actionController = actionController;
        }

        public void Execute(FlyingActorFlyToCommand command)
        {
            Vector3 targetPosition = new GameBoxVector3(
                command.GameBoxXPosition,
                command.GameBoxYPosition,
                command.GameBoxZPosition).ToUnityPosition();

            // If actor is inactive or at it's init position, just teleport to target position
            if (!_actorController.IsActive ||
                Vector3.Distance(Transform.Position, NpcInfoFactory.ActorInitPosition) < float.Epsilon)
            {
                Transform.Position = targetPosition;
                return;
            }

            // In case the target position is too far away
            if (Vector3.Distance(Transform.Position, targetPosition) > MAX_TARGET_DISTANCE)
            {
                Transform.Position = (Transform.Position - targetPosition).normalized * MAX_TARGET_DISTANCE + targetPosition;
            }

            var waiter = new WaitUntilCanceled();
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(waiter));

            var distance = (targetPosition - Transform.Position).magnitude;
            var duration = distance / DefaultFlySpeed;

            _actionController.PerformAction(distance < FLYING_MOVEMENT_MODE_SWITCH_DISTANCE
                ? _actor.GetMovementAction(MovementMode.Walk)
                : _actor.GetMovementAction(MovementMode.Run));

            StartCoroutine(FlyToAsync(targetPosition, duration, () => waiter.CancelWait()));
        }

        private IEnumerator FlyToAsync(Vector3 targetPosition, float duration, Action onFinished = null)
        {
            Vector3 oldPosition = Transform.Position;

            // Facing towards target position, ignoring y
            Transform.LookAt(new Vector3(targetPosition.x, oldPosition.y, targetPosition.z));

            var timePast = 0f;
            while (timePast < duration)
            {
                Vector3 newPosition = oldPosition;
                newPosition += (timePast / duration) * (targetPosition - oldPosition);

                Transform.Position = newPosition;

                timePast += _gameTimeProvider.DeltaTime;
                yield return null;
            }

            Transform.Position = targetPosition;
            _actionController.PerformAction(_actor.GetIdleAction());
            onFinished?.Invoke();
        }
    }
}