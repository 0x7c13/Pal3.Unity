// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Script.Waiter
{
    public class WaitUntilCanceled : IScriptRunnerWaiter
    {
        private bool _shouldWait = true;

        public void CancelWait()
        {
            _shouldWait = false;
        }

        public bool ShouldWait(float deltaTime = 0)
        {
            return _shouldWait;
        }
    }
}