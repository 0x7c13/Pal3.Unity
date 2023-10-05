// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Script.Waiter
{
    /// <summary>
    /// Waiter to stop script runner from executing next command
    /// until condition is met.
    /// </summary>
    public interface IScriptRunnerWaiter
    {
        /// <summary>
        /// Should script keep waiting?
        /// </summary>
        /// <param name="deltaTime">Game time past since last check</param>
        /// <returns>True if script runner should keep waiting</returns>
        public bool ShouldWait(float deltaTime = 0f);
    }
}