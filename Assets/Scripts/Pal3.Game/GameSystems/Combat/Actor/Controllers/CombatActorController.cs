// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.GameSystems.Combat.Actor.Controllers
{
    using System;
    using System.Collections;
    using Combat;
    using Engine.Animation;
    using Engine.Core.Implementation;
    using Game.Actor.Controllers;
    using Scene;

    using Quaternion = UnityEngine.Quaternion;
    using Vector3 = UnityEngine.Vector3;

    public sealed class CombatActorController : GameEntityScript
    {
        private const string COMBAT_ANIMATION_NORMAL_ATTACK_EVENT_NAME_PREFIX = "work";
        private const string COMBAT_ANIMATION_JUMP_ATTACK_EVENT_NAME_PREFIX = "flydown";
        
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

        private void AnimationEventTriggered(object sender, string eventName)
        {
            if (eventName.StartsWith(COMBAT_ANIMATION_NORMAL_ATTACK_EVENT_NAME_PREFIX, StringComparison.Ordinal))
            {
                // TODO: Impl normal attack behavior
            }
            else if (eventName.StartsWith(COMBAT_ANIMATION_JUMP_ATTACK_EVENT_NAME_PREFIX, StringComparison.Ordinal))
            {
                // TODO: Impl jump attack behavior
            }
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
            if (_actionController is VertexAnimationActorActionController vertexActionController)
            {
                vertexActionController.AnimationEventTriggered -= AnimationEventTriggered;
                vertexActionController.AnimationEventTriggered += AnimationEventTriggered;
            }

            _actionController.PerformAction(_actor.GetPreAttackAction());
        }

        private void DeActivate()
        {
            if (_actionController is VertexAnimationActorActionController vertexActionController)
            {
                vertexActionController.AnimationEventTriggered -= AnimationEventTriggered;
            }
            
            _actionController.DeActivate();
        }

        public IEnumerator StartNormalAttackAsync(CombatActorController enemyActorController,
            CombatScene combatScene)
        {
            Quaternion currentRotation = _actionController.Transform.Rotation;

            _actionController.PerformAction(_actor.GetMovementAction());

            Vector3 enemySize = enemyActorController.GetActionController().GetMeshBounds().size;
            float enemyRadius = MathF.Max(enemySize.x, enemySize.z) / 2f;

            Vector3 mySize = _actionController.GetMeshBounds().size;
            float myRadius = MathF.Max(mySize.x, mySize.z) / 2f;

            Vector3 targetElementPosition = combatScene.GetWorldPosition(enemyActorController.GetElementPosition());
            Vector3 myElementPosition = combatScene.GetWorldPosition(GetElementPosition());

            targetElementPosition += (myElementPosition - targetElementPosition).normalized * (enemyRadius + myRadius);

            float distance = Vector3.Distance(targetElementPosition, myElementPosition);
            float duration = distance / _actor.GetMovementSpeed();

            _actionController.Transform.LookAt(targetElementPosition);
            yield return Transform.MoveAsync(targetElementPosition, duration);
            
            yield return _actionController.PerformActionAsync(_actor.GetAttackAction());

            _actionController.PerformAction(_actor.GetMovementAction());
            _actionController.Transform.LookAt(myElementPosition);
            yield return Transform.MoveAsync(myElementPosition, duration);

            _actionController.PerformAction(_actor.GetPreAttackAction());

            _actionController.Transform.Rotation = currentRotation;
        }
    }
}