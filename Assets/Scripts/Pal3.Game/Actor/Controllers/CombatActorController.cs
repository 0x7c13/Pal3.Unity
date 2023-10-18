// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Actor.Controllers
{
    using System;
    using System.Collections;
    using Engine.Animation;
    using Engine.Core.Implementation;
    using Engine.Coroutine;
    using GameSystems.Combat;
    using Scene;
    using Script.Waiter;

    using Quaternion = UnityEngine.Quaternion;
    using Vector3 = UnityEngine.Vector3;

    public sealed class CombatActorController : GameEntityScript
    {
        private CombatActor _actor;
        private ActorActionController _actionController;
        private ElementPosition _elementPosition;

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

        public void Init(CombatActor actor,
            ActorActionController actionController,
            ElementPosition elementPosition)
        {
            _actor = actor;
            _actionController = actionController;
            _elementPosition = elementPosition;
        }

        public ElementPosition GetElementPosition()
        {
            return _elementPosition;
        }

        public void ChangeElementPosition(ElementPosition elementPosition)
        {
            _elementPosition = elementPosition;
        }

        public ActorActionController GetActionController()
        {
            return _actionController;
        }

        private void Activate()
        {
            _actionController.PerformAction(_actor.GetPreAttackAction());
        }

        private void DeActivate()
        {
            _actionController.DeActivate();
        }

        public IEnumerator StartNormalAttackAsync(CombatActorController combatActorController,
            CombatScene combatScene)
        {
            Quaternion currentRotation = _actionController.Transform.Rotation;

            _actionController.PerformAction(_actor.GetMovementAction());

            var enemySize = combatActorController.GetActionController().GetMeshBounds().size;
            var enemyRadius = MathF.Max(enemySize.x, enemySize.z) / 2f;

            var mySize = _actionController.GetMeshBounds().size;
            var myRadius = MathF.Max(mySize.x, mySize.z) / 2f;

            var targetElementPosition = combatScene.GetWorldPosition(combatActorController.GetElementPosition());
            var myElementPosition = combatScene.GetWorldPosition(GetElementPosition());

            targetElementPosition += (myElementPosition - targetElementPosition).normalized * (enemyRadius + myRadius);

            var distance = Vector3.Distance(targetElementPosition, myElementPosition);
            var duration = distance / _actor.GetMovementSpeed();

            _actionController.Transform.LookAt(targetElementPosition);
            yield return Transform.MoveAsync(targetElementPosition, duration);

            WaitUntilCanceled waiter = new WaitUntilCanceled();
            _actionController.PerformAction(_actor.GetAttackAction(), overwrite: true, loopCount: 1, waiter);

            yield return CoroutineYieldInstruction.WaitUntil(() => !waiter.ShouldWait());

            _actionController.PerformAction(_actor.GetMovementAction());
            _actionController.Transform.LookAt(myElementPosition);
            yield return Transform.MoveAsync(myElementPosition, duration);

            _actionController.PerformAction(_actor.GetPreAttackAction());

            _actionController.Transform.Rotation = currentRotation;
        }
    }
}