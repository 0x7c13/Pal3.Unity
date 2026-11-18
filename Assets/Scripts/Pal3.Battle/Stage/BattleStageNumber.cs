// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;

    // ShowDamage and ShowMiss have the same lifetime and one activation callback.
    // The caller owns the original RNG draw, numeric offset, and displayed value.
    public sealed class BattleStageNumber : IBattleStageJob
    {
        private readonly Action<BattleStageContext> _onStart;
        private readonly Action<BattleStageContext> _onBodyBlock;

        public float Duration
        {
            get;
        }

        public bool Started
        {
            get; private set;
        }

        public float StartTime
        {
            get; private set;
        }

        public bool Finished
        {
            get; private set;
        }

        public BattleStageNumber(float duration, Action<BattleStageContext> onStart,
            Action<BattleStageContext> onBodyBlock = null)
        {
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration < 0f)
                throw new ArgumentOutOfRangeException(nameof(duration));
            Duration = duration;
            _onStart = onStart ?? throw new ArgumentNullException(nameof(onStart));
            _onBodyBlock = onBodyBlock;
        }

        public bool Advance(BattleStageContext context)
        {
            if (Finished)
                return true;
            if (!Started)
            {
                Started = true;
                StartTime = context.CreatedTime;
                if (_onBodyBlock != null)
                {
                    _onBodyBlock(context);
                    // A blocking body subtask finishes immediately; native DoTask
                    // starts ShowDamage later in this same pass, resetting its time.
                    StartTime = context.Time;
                }
                _onStart(context);
            }
            // Both operands are stored floats, but x87 retains the subtraction
            // through the strict comparison. A float subtraction loses a tick.
            Finished = (double)context.Time - StartTime > Duration;
            return Finished;
        }
    }
}
