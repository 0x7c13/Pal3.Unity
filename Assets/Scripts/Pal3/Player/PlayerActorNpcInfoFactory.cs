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

    public static class PlayerActorNpcInfoFactory
    {
        // This is a position that is far away from the camera to prevent glitches.
        public static readonly Vector3 ActorInitPosition = new (777f, 777f, 777f); // Unity position

        public static ScnNpcInfo Create(PlayerActorId actorId)
        {
            Vector3 gameBoxInitPosition = GameBoxInterpreter.ToGameBoxPosition(ActorInitPosition);

            return new ScnNpcInfo
            {
                Id = (byte)actorId,
                Name = ActorConstants.MainActorNameMap[actorId],
                Kind = ScnActorKind.MainActor,
                InitActive = 0,
                InitAction = ActorConstants.ActionNames[ActorActionType.Stand],
                InitBehaviour = ScnActorBehaviour.None,
                GameBoxXPosition = gameBoxInitPosition.x,
                GameBoxYPosition = gameBoxInitPosition.y,
                GameBoxZPosition = gameBoxInitPosition.z,
            };
        }

        public static IEnumerable<ScnNpcInfo> CreateAll()
        {
            return from actorId in (PlayerActorId[]) Enum.GetValues(typeof(PlayerActorId)) select Create(actorId);
        }
    }
}