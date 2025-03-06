// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Coroutine
{
    using System;
    using UnityEngine;

    /// <summary>
    /// A static class that provides yield instructions for coroutines.
    /// </summary>
    public static class CoroutineYieldInstruction
    {
        /// <summary>
        /// Waits for the specified number of seconds before continuing the coroutine.
        /// </summary>
        /// <param name="seconds">The number of seconds to wait.</param>
        /// <returns>A yield instruction that waits for the specified number of seconds.</returns>
        public static object WaitForSeconds(float seconds)
        {
            return new WaitForSeconds(seconds);
        }

        /// <summary>
        /// Waits until the specified predicate returns true before continuing the coroutine.
        /// </summary>
        /// <param name="predicate">The predicate to evaluate.</param>
        /// <returns>A yield instruction that waits until the predicate returns true.</returns>
        public static object WaitUntil(Func<bool> predicate)
        {
            return new WaitUntil(predicate);
        }
    }
}