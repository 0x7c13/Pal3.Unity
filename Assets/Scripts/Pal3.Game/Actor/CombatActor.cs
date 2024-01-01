// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Actor
{
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Gdb;
    using Data;

    public sealed class CombatActor : ActorBase
    {
        public CombatActorInfo Info { get; }

        public CombatActor(GameResourceProvider resourceProvider, CombatActorInfo actorInfo) :
            base(resourceProvider, (int)actorInfo.Id,
                actorInfo.Type == CombatActorType.MainActor ? actorInfo.Id.ToString() : actorInfo.ModelId)
        {
            Info = actorInfo;
        }

        public string GetPreAttackAction()
        {
            return Info.Type == CombatActorType.MainActor ?
                ActorConstants.ActionToNameMap[ActorActionType.PreAttack] :
                ActorConstants.ActionToNameMap[ActorActionType.NpcStand1];
        }

        public string GetAttackAction()
        {
            return Info.Type == CombatActorType.MainActor ?
                ActorConstants.ActionToNameMap[ActorActionType.Attack1] :
                ActorConstants.ActionToNameMap[ActorActionType.NpcAttack];
        }

        public string GetMovementAction()
        {
            return Info.Type == CombatActorType.MainActor ?
                ActorConstants.ActionToNameMap[ActorActionType.AttackMove] :
                ActorConstants.ActionToNameMap[ActorActionType.NpcRun];
        }

        public float GetMovementSpeed()
        {
            return 12f;
        }
    }
}