// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

#if PAL3A
namespace Pal3.Game.Command.Extensions
{
    using Core.Command;
    [AvailableInConsole]
    public sealed class Pal3ARestoreCombatScriptStateCommand : ICommand
    {
        public Pal3ARestoreCombatScriptStateCommand(int flags, int lostHp, int lostMp)
        {
            Flags = flags;
            LostHp = lostHp;
            LostMp = lostMp;
        }

        public int Flags { get; }

        public int LostHp { get; }

        public int LostMp { get; }
    }
}
#endif
