// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Actor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Contracts;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using MetaData;
    using UnityEngine;

    public static class NpcInfoFactory
    {
        // This is a position that is far away from the camera to prevent glitches.
        public static readonly Vector3 ActorInitPosition = new (77f, 77f, 77f); // Unity position

        public static IEnumerable<ScnNpcInfo> CreateAllPlayerActorNpcInfos()
        {
            return from actorId in (PlayerActorId[]) Enum.GetValues(typeof(PlayerActorId))
                select CreateActorNpcInfo((byte) actorId, ActorConstants.MainActorNameMap[actorId], ActorType.MainActor);
        }

        #if PAL3A
        public static IEnumerable<ScnNpcInfo> CreateAllFengYaSongNpcInfos()
        {
            return from actorId in (FengYaSongActorId[]) Enum.GetValues(typeof(FengYaSongActorId))
                select CreateActorNpcInfo((byte)actorId, ActorConstants.FengYaSongActorNameMap[actorId], ActorType.StoryNpc);
        }
        #endif

        private static ScnNpcInfo CreateActorNpcInfo(byte actorId, string actorName, ActorType actorType)
        {
            Vector3 gameBoxInitPosition = GameBoxInterpreter.ToGameBoxPosition(ActorInitPosition);

            return new ScnNpcInfo
            {
                Id = actorId,
                Name = actorName,
                Type = actorType,
                InitActive = 0,
                InitAction = ActorConstants.ActionToNameMap[ActorActionType.Stand],
                InitBehaviour = ActorBehaviourType.None,
                GameBoxXPosition = gameBoxInitPosition.x,
                GameBoxYPosition = gameBoxInitPosition.y,
                GameBoxZPosition = gameBoxInitPosition.z,
            };
        }
    }
}