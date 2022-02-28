// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Player
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.DataReader.Scn;
    using MetaData;

    public static class PlayerActorNpcInfo
    {
        public static ScnNpcInfo Get(PlayerActorId actorId)
        {
            return new ScnNpcInfo()
            {
                Id = (byte)actorId,
                Name = ActorConstants.MainActorNameMap[actorId],
                Kind = ScnActorKind.MainActor,
                InitActive = 0,
                InitAction = ActorConstants.ActionNames[ActorActionType.Stand]
            };
        }

        public static IEnumerable<ScnNpcInfo> GetAll()
        {
            return from actorId in (PlayerActorId[]) Enum.GetValues(typeof(PlayerActorId)) select Get(actorId);
        }
    }
}