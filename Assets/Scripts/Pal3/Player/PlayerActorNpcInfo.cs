// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Player
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using MetaData;
    using UnityEngine;

    public static class PlayerActorNpcInfo
    {
        public static readonly Vector3 InitPosition = new Vector3(777f, 777f, 777f);
        
        public static ScnNpcInfo Get(PlayerActorId actorId)
        {
            var initPosition = GameBoxInterpreter.ToGameBoxPosition(InitPosition);
            return new ScnNpcInfo
            {
                Id = (byte)actorId,
                Name = ActorConstants.MainActorNameMap[actorId],
                Kind = ScnActorKind.MainActor,
                InitActive = 0,
                InitAction = ActorConstants.ActionNames[ActorActionType.Stand],
                PositionX = initPosition.x,
                PositionY = initPosition.y,
                PositionZ = initPosition.z,
            };
        }

        public static IEnumerable<ScnNpcInfo> GetAll()
        {
            return from actorId in (PlayerActorId[]) Enum.GetValues(typeof(PlayerActorId)) select Get(actorId);
        }
    }
}