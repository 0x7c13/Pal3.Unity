// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Coroutine
{
    using System;
    using UnityEngine;

    public static class CoroutineYieldInstruction
    {
        public static object WaitForSeconds(float seconds)
        {
            return new WaitForSeconds(seconds);
        }

        public static object WaitUntil(Func<bool> predicate)
        {
            return new WaitUntil(predicate);
        }
    }
}