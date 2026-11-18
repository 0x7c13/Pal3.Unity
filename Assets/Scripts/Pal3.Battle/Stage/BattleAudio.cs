// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    // Optional output port for one-shot battle cues; timing belongs to the caller.
    public interface IBattleAudio
    {
        void PlaySound(string sound);
    }
}
