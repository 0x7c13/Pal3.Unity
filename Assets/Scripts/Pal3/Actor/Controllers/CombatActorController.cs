// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Actor.Controllers
{
    using System.Collections;
    using Core.Animation;
    using GameSystems.Combat;
    using Scene;
    using Script.Waiter;
    using UnityEngine;

    public sealed class CombatActorController : MonoBehaviour
    {
        private CombatActor _actor;
        private ActorActionController _actionController;
        private CombatSceneElementPosition _elementPosition;

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

        public void Init(CombatActor actor,
            ActorActionController actionController,
            CombatSceneElementPosition elementPosition)
        {
            _actor = actor;
            _actionController = actionController;
            _elementPosition = elementPosition;
        }

        public CombatSceneElementPosition GetElementPosition()
        {
            return _elementPosition;
        }

        public void ChangeElementPosition(CombatSceneElementPosition elementPosition)
        {
            _elementPosition = elementPosition;
        }

        private void Activate()
        {
            _actionController.PerformAction(_actor.GetPreAttackAction());
        }

        private void DeActivate()
        {
            _actionController.DeActivate();
        }

        // public IEnumerator StartNormalAttackAsync(CombatActorController combatActorController,
        //     CombatScene combatScene)
        // {
        //     Quaternion currentRotation = _actionController.transform.rotation;
        //
        //     _actionController.PerformAction(_actor.GetCombatMovementAction());
        //
        //     var targetElementPosition = combatScene.GetWorldPosition(combatActorController.GetElementPosition());
        //     var myElementPosition = combatScene.GetWorldPosition(GetElementPosition());
        //     var distance = Vector3.Distance(targetElementPosition, myElementPosition);
        //     var duration = distance / _actor.GetCombatMovementSpeed();
        //
        //     _actionController.transform.LookAt(targetElementPosition);
        //     yield return transform.MoveAsync(targetElementPosition, duration);
        //
        //     WaitUntilCanceled waiter = new WaitUntilCanceled();
        //     _actionController.PerformAction(_actor.GetCombatAttackAction(), overwrite: true, loopCount: 1, waiter);
        //
        //     yield return new WaitUntil(() => !waiter.ShouldWait());
        //
        //     _actionController.PerformAction(_actor.GetCombatMovementAction());
        //     _actionController.transform.LookAt(myElementPosition);
        //     yield return transform.MoveAsync(myElementPosition, duration);
        //
        //     _actionController.PerformAction(_actor.GetPreAttackAction());
        //
        //     _actionController.transform.rotation = currentRotation;
        // }
    }
}