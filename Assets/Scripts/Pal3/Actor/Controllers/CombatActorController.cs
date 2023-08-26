// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Actor.Controllers
{
    using UnityEngine;

    public sealed class CombatActorController : MonoBehaviour
    {
        private CombatActor _actor;
        private ActorActionController _actionController;

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
            ActorActionController actionController)
        {
            _actor = actor;
            _actionController = actionController;
        }

        private void Activate()
        {
            _actionController.PerformAction(_actor.GetPreAttackAction());
        }

        private void DeActivate()
        {
            _actionController.DeActivate();
        }
    }
}