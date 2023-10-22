// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Script.Waiter
{
    public sealed class WaitUntilTime : IScriptRunnerWaiter
    {
        private float _totalTimeInSec;

        public WaitUntilTime(float totalTimeInSec)
        {
            _totalTimeInSec = totalTimeInSec;
        }

        public bool ShouldWait(float deltaTime = 0)
        {
            _totalTimeInSec -= deltaTime;

            // Prevent overflow
            if (_totalTimeInSec < 0)
            {
                _totalTimeInSec = -1f;
            }

            return _totalTimeInSec > 0;
        }
    }
}