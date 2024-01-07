// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command
{
    /// <summary>
    /// Interface for a command preprocessor that can modify a command before it is executed.
    /// </summary>
    public interface ISceCommandPreprocessor
    {
        /// <summary>
        /// Processes the given command before it is executed.
        /// </summary>
        /// <param name="command">The command to process.</param>
        /// <param name="currentPlayerActorId">The ID of the current player actor.</param>
        public void Process(ICommand command, int currentPlayerActorId);
    }
}