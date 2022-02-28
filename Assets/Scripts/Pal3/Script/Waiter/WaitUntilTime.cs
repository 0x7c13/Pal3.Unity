// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Script.Waiter
{
    public class WaitUntilTime : IWaitUntil
    {
        private float _totalTimeInSec;

        public WaitUntilTime(float totalTimeInSec)
        {
            _totalTimeInSec = totalTimeInSec;
        }

        public bool ShouldWait(float deltaTime = 0)
        {
            _totalTimeInSec -= deltaTime;
            return _totalTimeInSec > 0;
        }
    }
}