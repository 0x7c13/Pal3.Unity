// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Actor
{
    using Core.DataReader.Gdb;
    using Data;

    public sealed class CombatActor : ActorBase
    {
        public CombatActorInfo Info { get; }

        public CombatActor(GameResourceProvider resourceProvider, CombatActorInfo actorInfo) :
            base(resourceProvider, (int)actorInfo.Id, actorInfo.Name)
        {
            Info = actorInfo;
        }
    }
}