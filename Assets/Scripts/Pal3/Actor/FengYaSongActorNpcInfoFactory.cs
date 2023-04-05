// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3A

namespace Pal3.Actor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.DataReader.Scn;
    using MetaData;

    public class FengYaSongActorNpcInfoFactory
    {
        public static ScnNpcInfo Create(FengYaSongActorId actorId)
        {
            return new ScnNpcInfo
            {
                Id = (byte)actorId,
                Name = ActorConstants.FengYaSongActorNameMap[actorId],
                Kind = ScnActorKind.StoryNpc,
                InitActive = 0,
                InitAction = ActorConstants.ActionToNameMap[ActorActionType.Stand],
            };
        }

        public static IEnumerable<ScnNpcInfo> CreateAll()
        {
            return from actorId in (FengYaSongActorId[]) Enum.GetValues(typeof(FengYaSongActorId)) select Create(actorId);
        }
    }
}

#endif