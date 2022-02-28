// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Script.Waiter
{
    public class WaitUntilCanceled : IWaitUntil
    {
        // TODO: Remove this
        public object Tag { get; }

        private bool _shouldWait = true;

        public WaitUntilCanceled(object tag)
        {
            Tag = tag;
        }

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