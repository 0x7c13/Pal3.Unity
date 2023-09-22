// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.Extensions
{
    using Core.Command;

    public class CombatActorCollideWithPlayerActorNotification : ICommand
    {
        public CombatActorCollideWithPlayerActorNotification(
            int combatActorId,
            int playerActorId)
        {
            CombatActorId = combatActorId;
            PlayerActorId = playerActorId;
        }

        public int CombatActorId { get; }
        public int PlayerActorId { get; }
    }
}