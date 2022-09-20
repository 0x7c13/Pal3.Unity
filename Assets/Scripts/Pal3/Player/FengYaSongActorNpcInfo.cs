// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3A

namespace Pal3.Player
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.DataReader.Scn;
    using MetaData;

    public class FengYaSongActorNpcInfo
    {
        public static ScnNpcInfo Get(FengYaSongActorId actorId)
        {
            return new ScnNpcInfo
            {
                Id = (byte)actorId,
                Name = ActorConstants.FengYaSongActorNameMap[actorId],
                Kind = ScnActorKind.StoryNpc,
                InitActive = 0,
                InitAction = ActorConstants.ActionNames[ActorActionType.Stand],
            };
        }
        
        public static IEnumerable<ScnNpcInfo> GetAll()
        {
            return from actorId in (FengYaSongActorId[]) Enum.GetValues(typeof(FengYaSongActorId)) select Get(actorId);
        }
    }
}

#endif