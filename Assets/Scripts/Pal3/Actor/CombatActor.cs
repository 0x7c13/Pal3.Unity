// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Actor
{
    using Core.Contracts;
    using Core.DataReader.Gdb;
    using Data;
    using MetaData;

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
            if (Info.Type == CombatActorType.MainActor)
            {
                return ActorConstants.ActionToNameMap[ActorActionType.PreAttack];
            }
            else
            {
                return ActorConstants.ActionToNameMap[ActorActionType.NpcStand1];
            }
        }
    }
}