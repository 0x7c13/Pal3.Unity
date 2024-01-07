// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Actor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Core.Primitives;
    using Engine.Extensions;

    using Vector3 = UnityEngine.Vector3;

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
            GameBoxVector3 gameBoxInitPosition = ActorInitPosition.ToGameBoxPosition();

            return new ScnNpcInfo
            {
                Id = actorId,
                Name = actorName,
                Type = actorType,
                InitActive = 0,
                InitAction = ActorConstants.ActionToNameMap[ActorActionType.Stand],
                InitBehaviour = ActorBehaviourType.None,
                GameBoxXPosition = gameBoxInitPosition.X,
                GameBoxYPosition = gameBoxInitPosition.Y,
                GameBoxZPosition = gameBoxInitPosition.Z,
            };
        }
    }
}