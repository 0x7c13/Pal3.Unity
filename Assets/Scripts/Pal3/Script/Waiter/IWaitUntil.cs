// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Script.Waiter
{
    public interface IWaitUntil
    {
        public bool ShouldWait(float deltaTime = 0f);
    }
}