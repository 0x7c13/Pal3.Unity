// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using UnityEngine;

    public interface IBattleMovieAudioTransport
    {
        void SynchronizeMovie(object owner, AudioClip clip, double elapsed, bool playing);

        void StopMovie(object owner);
    }
}
