// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;

    // CEffectSimple: one fixed XZ quad, sixteen 64x64 cells, one 50 fps loop.
    public sealed class BattleHitFlashSnapshot
    {
        public long Id
        {
            get;
        }

        public string Actor
        {
            get;
        }

        public BattleMovementPoint Position
        {
            get;
        }

        public int Frame
        {
            get;
        }

        public const float HalfSize = 100;

        public const int FrameCount = 16;

        internal BattleHitFlashSnapshot(long id, string actor, BattleMovementPoint position, int frame)
        {
            Id = id;
            Actor = actor;
            Position = position;
            Frame = frame;
        }

        // PAL3::GAME_COMBAT updates the global simple particles after the role
        // pass that creates them. The creation pass already advances one cell.
        public static int FrameAt(long elapsedTicks) => elapsedTicks < 0 ? -1 :
            checked((int)Math.Round((elapsedTicks + 1) * BattleSession.StepSeconds * 50, MidpointRounding.ToEven));
    }
}
