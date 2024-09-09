// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
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

            Vector3 enemySize = combatActorController.GetActionController().GetMeshBounds().size;
            float enemyRadius = MathF.Max(enemySize.x, enemySize.z) / 2f;

            Vector3 mySize = _actionController.GetMeshBounds().size;
            float myRadius = MathF.Max(mySize.x, mySize.z) / 2f;

            Vector3 targetElementPosition = combatScene.GetWorldPosition(combatActorController.GetElementPosition());
            Vector3 myElementPosition = combatScene.GetWorldPosition(GetElementPosition());

            targetElementPosition += (myElementPosition - targetElementPosition).normalized * (enemyRadius + myRadius);

            float distance = Vector3.Distance(targetElementPosition, myElementPosition);
            float duration = distance / _actor.GetMovementSpeed();

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