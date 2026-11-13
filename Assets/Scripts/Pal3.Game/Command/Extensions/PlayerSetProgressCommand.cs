// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Command.Extensions
{
    using Core.Command;
    [AvailableInConsole]
    public sealed class PlayerSetProgressCommand : ICommand
    {
        public PlayerSetProgressCommand(int actorId, int level, int weaponItemId)
        {
            ActorId = actorId;
            Level = level;
            WeaponItemId = weaponItemId;
        }

        public int ActorId { get; }

        public int Level { get; }

        public int WeaponItemId { get; }
    }
}
